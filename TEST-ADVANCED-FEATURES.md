# Advanced Features - Testing Guide

## ✅ Database Setup Complete!

The database has been fully set up with:
- ✅ Base schema (Users, Accounts, Cards, Transactions, KYC docs, etc.)
- ✅ Advanced features (FailureReason, RetryCount, RiskScore, RiskLevel, StatusChangedAt)
- ✅ New tables (MerchantAccounts, FraudDetectionRules, TransactionStatusHistory)
- ✅ All indexes and constraints

## 🚀 API Endpoints Available

### Payment Endpoints
1. **GET /api/payments/qr/{accountNumber}**
   - Generate QR code for merchant account
   - Example: `GET /api/payments/qr/ACC123456`

2. **POST /api/payments/initiate**
   - Initiate payment from NFC/QR scan
   - Body: `{ "toAccountNumber": "ACC123456", "amount": 100.00, "purpose": "Payment", "idempotencyKey": "key-123", "merchantId": "M001" }`

3. **GET /api/payments/pay?to={accountNumber}&merchant={merchantId}&name={name}**
   - NFC URL handler

### Transaction Endpoints (Enhanced)
1. **POST /api/transactions/transfer** (existing, enhanced)
   - Now includes fraud detection and enhanced status lifecycle
   - Response includes: `riskScore`, `riskLevel`, `retryRecommended`, etc.

2. **POST /api/transactions/{transactionId}/retry** (NEW)
   - Retry failed transaction
   - Only works for temporary failures

3. **GET /api/transactions/account/{accountId}** (existing, enhanced)
   - Response now includes advanced fields

### Admin Endpoints
1. **GET /api/admin/transactions/risk-review**
   - List high-risk transactions
   - Query params: `riskLevel=HIGH&status=Pending&pageNumber=1&pageSize=20`

2. **POST /api/admin/transactions/{transactionId}/review**
   - Approve/reject high-risk transaction
   - Body: `{ "action": "approve" | "reject", "reason": "..." }`

## 🧪 Test Scenarios

### 1. Test NFC/QR Payment
```bash
# 1. Generate QR code
GET /api/payments/qr/ACC123456

# 2. Initiate payment
POST /api/payments/initiate
{
  "toAccountNumber": "ACC123456",
  "amount": 50.00,
  "purpose": "Test QR payment",
  "idempotencyKey": "test-qr-001"
}
```

### 2. Test Fraud Detection
```bash
# Create a large transaction to trigger fraud detection
POST /api/transactions/transfer
{
  "fromAccountId": 1,
  "toAccountNumber": "ACC123456",
  "amount": 6000.00,  # Exceeds max single transfer (5000)
  "purpose": "Test fraud",
  "idempotencyKey": "test-fraud-001"
}
# Should return status: "Pending" with riskLevel: "HIGH"
```

### 3. Test Retry Mechanism
```bash
# 1. Create a transaction that fails (e.g., network timeout simulation)
# 2. Retry it
POST /api/transactions/{transactionId}/retry
```

### 4. Test Admin Review
```bash
# 1. List high-risk transactions
GET /api/admin/transactions/risk-review?riskLevel=HIGH&status=Pending

# 2. Approve a transaction
POST /api/admin/transactions/{transactionId}/review
{
  "action": "approve",
  "reason": "Verified customer"
}
```

## 📊 Background Jobs

Three background services are running:
1. **TransactionProcessingHostedService** - Processes pending transactions (every 1 minute)
2. **TransactionExpirationHostedService** - Expires old pending transactions (every 1 hour)
3. **TransactionAutoRetryHostedService** - Auto-retries failed transactions (every 5 minutes)

## 🔍 Verification

To verify everything is working:

1. **Check Swagger**: http://localhost:5000/swagger
2. **Test a transaction**: Create a transfer and check the response includes new fields
3. **Check database**: Verify new columns exist in Transactions table
4. **Check logs**: Background jobs should be processing transactions

## 📝 Notes

- All advanced features are fully implemented and ready to use
- The API will automatically use the new features
- Background jobs start automatically when the API runs
- Fraud detection rules are configurable (currently hardcoded, can be moved to database)
