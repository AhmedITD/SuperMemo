# Advanced Features Implementation Summary

This document summarizes the backend implementation of advanced features for the Virtual Banking API.

## Overview

The following advanced features have been implemented:

1. **NFC/QR Payment Initiation** - Account-based payment initiation via NFC tags and QR codes
2. **Enhanced Transaction Status Lifecycle** - Expanded status values with state machine validation
3. **Fraud Detection Hooks** - Rule-based fraud detection with risk scoring
4. **Failure Recovery & Retry Mechanisms** - Classification of failures and retry support
5. **Offline Queue Support** - Backend handling of offline queue sync (via idempotency)

## Database Changes

### Migration Script

A SQL migration script has been created at:
- `SuperMemo.Infrastructure/Migrations/20250207000000_AdvancedFeatures.sql`

**Important:** Before running the migration, note that the `TransactionStatus` enum values have changed:
- Old: `Created = 0, Sending = 1, Completed = 2, Failed = 3`
- New: `Created = 0, Pending = 1, Sending = 2, Completed = 3, Failed = 4, Expired = 5`

If you have existing transactions with `Status = 1` (previously "Sending"), you may need to update them:
```sql
UPDATE "Transactions" SET "Status" = 2 WHERE "Status" = 1;
```

### New Tables

1. **MerchantAccounts** - Optional table for managing merchant accounts
2. **FraudDetectionRules** - Optional table for configurable fraud rules
3. **TransactionStatusHistory** - Optional audit trail for status changes

### Enhanced Transactions Table

New columns added:
- `FailureReason` (integer, nullable) - Enum value for failure classification
- `RetryCount` (integer, default 0) - Number of retry attempts
- `RiskScore` (integer, nullable) - Fraud risk score (0-100)
- `RiskLevel` (integer, nullable) - Risk level enum (LOW, MEDIUM, HIGH)
- `StatusChangedAt` (timestamp, nullable) - When status last changed

New indexes:
- `IX_Transactions_Status_StatusChangedAt` - For expiration queries
- `IX_Transactions_RiskLevel_Status` - For fraud review queries

## New Services

### 1. TransactionStatusMachine
- **Location:** `SuperMemo.Application/Services/TransactionStatusMachine.cs`
- **Purpose:** Enforces valid status transitions
- **Key Methods:**
  - `IsValidTransition()` - Validates transition
  - `TransitionTo()` - Performs transition with history logging

### 2. FraudDetectionService
- **Location:** `SuperMemo.Application/Services/FraudDetectionService.cs`
- **Purpose:** Calculates fraud risk scores
- **Rules:**
  - Amount threshold (daily limit: 10,000, max single: 5,000)
  - Velocity check (> 10 transactions in 1 minute)
  - New device detection
  - New card detection (< 24 hours old)
- **Risk Levels:**
  - 0-30: LOW
  - 31-70: MEDIUM
  - 71-100: HIGH

### 3. FailureClassificationService
- **Location:** `SuperMemo.Application/Services/FailureClassificationService.cs`
- **Purpose:** Classifies failures as temporary or permanent
- **Temporary failures:** NetworkTimeout, ServiceUnavailable, ConcurrencyConflict
- **Permanent failures:** InsufficientFunds, InvalidDestination, RiskBlocked, AccountFrozen, AccountClosed

### 4. PaymentInitiationService
- **Location:** `SuperMemo.Application/Services/PaymentInitiationService.cs`
- **Purpose:** Handles NFC/QR payment initiation
- **Features:**
  - QR code generation for merchant accounts
  - Payment initiation from NFC/QR scans

### 5. TransactionProcessingService
- **Location:** `SuperMemo.Application/Services/TransactionProcessingService.cs`
- **Purpose:** Background processing of transactions
- **Methods:**
  - `ProcessPendingTransactionsAsync()` - Processes pending → sending → completed
  - `ExpirePendingTransactionsAsync()` - Expires old pending transactions
  - `AutoRetryFailedTransactionsAsync()` - Auto-retries temporary failures

## New API Endpoints

### Payment Endpoints (`/api/payments`)

1. **GET /api/payments/qr/{accountNumber}**
   - Generates QR code data for a merchant account
   - Returns: `QrCodeResponse` with QR data and merchant info

2. **POST /api/payments/initiate**
   - Initiates payment from NFC/QR scan
   - Request: `InitiatePaymentRequest` (toAccountNumber, amount, purpose, idempotencyKey, merchantId)
   - Returns: `TransactionResponse`

3. **GET /api/payments/pay** (AllowAnonymous)
   - NFC URL handler
   - Query params: `to`, `merchant`, `name`
   - Returns: Payment initiation data

### Transaction Endpoints (Enhanced)

1. **POST /api/transactions/{transactionId}/retry** (NEW)
   - Retries a failed transaction
   - Only works for temporary failures
   - Enforces max retry count (3)

### Admin Endpoints (`/api/admin/transactions`)

1. **GET /api/admin/transactions/risk-review**
   - Lists high-risk transactions pending review
   - Query params: `riskLevel` (default: High), `status` (default: Pending), `pageNumber`, `pageSize`
   - Returns: Paginated list of transactions

2. **POST /api/admin/transactions/{transactionId}/review**
   - Approves or rejects high-risk transaction
   - Request body: `{ "action": "approve" | "reject", "reason": "..." }`
   - Returns: Updated transaction

## Background Jobs

Three background hosted services have been created:

1. **TransactionProcessingHostedService**
   - Runs every 1 minute
   - Processes pending transactions (moves to Sending → Completed)

2. **TransactionExpirationHostedService**
   - Runs every 1 hour
   - Expires transactions in Pending status for > 24 hours

3. **TransactionAutoRetryHostedService**
   - Runs every 5 minutes
   - Automatically retries failed transactions with temporary failures (up to 3 retries)

All services are registered in `Program.cs` and start automatically with the application.

## Enhanced Transaction Lifecycle

### Status Flow

```
Created → Pending → Sending → Completed
         ↓         ↓
       Failed    Failed
         ↓
      Expired (auto, after 24h in Pending)
```

### Process Flow

1. **Transaction Creation:**
   - Status: `Created`
   - Validations: Account active, balance, KYC, approval, valid card
   - Fraud detection runs
   - Status moves to `Pending`

2. **Fraud Check:**
   - If HIGH risk → stays in `Pending` (admin review required)
   - If LOW/MEDIUM risk → ready for processing

3. **Processing (Background Job):**
   - Moves from `Pending` → `Sending`
   - Executes transfer (debit/credit)
   - Moves to `Completed` on success
   - Moves to `Failed` on error

4. **Failure Handling:**
   - Failure classified as temporary or permanent
   - Temporary failures can be retried (manual or auto)
   - Permanent failures require user action

## Enhanced DTOs

### TransactionResponse (Updated)

New fields:
- `FailureReason` - Enum value
- `RetryCount` - Number of retries
- `RiskScore` - 0-100
- `RiskLevel` - LOW/MEDIUM/HIGH
- `StatusChangedAt` - Timestamp
- `RetryRecommended` - Boolean
- `RetryAfterSeconds` - Suggested retry delay
- `MaxRetries` - Maximum retry count (3)

## Error Codes (New)

- `TEMPORARY_FAILURE` - Transaction failed but can be retried
- `PERMANENT_FAILURE` - Transaction failed and cannot be retried
- `INVALID_STATUS_TRANSITION` - Invalid status change attempted
- `HIGH_RISK_TRANSACTION` - Transaction flagged for review

## Security Considerations

1. **NFC/QR Security:**
   - Account numbers validated before processing
   - Rate limiting recommended on payment endpoints
   - Receiver account must exist and be active

2. **Fraud Detection:**
   - Rules and thresholds not exposed in API responses
   - All fraud events logged for analysis
   - Admins can override decisions (with audit trail)

3. **Retry Security:**
   - Only temporary failures can be retried
   - Max retry count enforced (3)
   - Same idempotency_key prevents duplicates

## Testing Recommendations

### Unit Tests
- Fraud detection rules (amount, velocity, new device, new card)
- Status machine transitions (valid and invalid)
- Failure classification (temporary vs permanent)
- Idempotency with same key

### Integration Tests
- NFC/QR payment initiation flow
- Transaction lifecycle (Created → Pending → Sending → Completed)
- Retry endpoint with same idempotency_key
- Auto-expiration of pending transactions
- Admin review workflow

## Configuration

Fraud detection rules are currently hardcoded in `FraudDetectionService`. To make them configurable:

1. Use the `FraudDetectionRules` table
2. Load rules from database in `FraudDetectionService`
3. Update rules via admin API

## Next Steps

1. **Run Database Migration:**
   ```bash
   # Apply the SQL migration script
   psql -d your_database -f SuperMemo.Infrastructure/Migrations/20250207000000_AdvancedFeatures.sql
   ```

2. **Update Existing Data (if needed):**
   ```sql
   -- Update old Sending status to new Sending (2)
   UPDATE "Transactions" SET "Status" = 2 WHERE "Status" = 1;
   ```

3. **Test the Implementation:**
   - Test NFC/QR payment flow
   - Test fraud detection with various scenarios
   - Test retry mechanism
   - Test admin review workflow

4. **Monitor Background Jobs:**
   - Check logs for transaction processing
   - Monitor expiration and retry jobs
   - Adjust intervals if needed

## Files Created/Modified

### New Files
- Domain entities: `MerchantAccount.cs`, `FraudDetectionRule.cs`, `TransactionStatusHistory.cs`
- Enums: `RiskLevel.cs`, `FailureReason.cs`
- Exceptions: `InvalidStatusTransitionException.cs`, `HighRiskTransactionException.cs`, `TemporaryFailureException.cs`, `PermanentFailureException.cs`
- Services: `TransactionStatusMachine.cs`, `FraudDetectionService.cs`, `FailureClassificationService.cs`, `PaymentInitiationService.cs`, `TransactionProcessingService.cs`
- Controllers: `PaymentController.cs`, `AdminTransactionController.cs`
- Background services: `TransactionProcessingHostedService.cs`
- DTOs: Payment request/response DTOs
- Migration: SQL migration script

### Modified Files
- `Transaction.cs` - Added new fields
- `TransactionStatus.cs` - Added Pending and Expired
- `TransactionService.cs` - Enhanced with fraud checks and lifecycle
- `TransactionResponse.cs` - Added new fields
- `TransactionConfiguration.cs` - Added new fields and indexes
- `SuperMemoDbContext.cs` - Added new DbSets
- `DependencyInjection.cs` - Registered new services
- `Program.cs` - Registered background services
- `ErrorCodes.cs` - Added new error codes
- `TransactionsController.cs` - Added retry endpoint

## Notes

- All code follows existing project patterns and conventions
- Services are properly registered in DI container
- Background jobs use the same pattern as `PayrollRunnerHostedService`
- Idempotency is maintained throughout for offline queue support
- Audit logging is integrated for all key operations
