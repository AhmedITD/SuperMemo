# SuperMemo Virtual Banking API - Frontend Integration Guide

**Base URL:** `https://kzcajtqhoz.a.pinggy.link`  
**API Version:** v1  
**Swagger Documentation:** https://kzcajtqhoz.a.pinggy.link/swagger/index.html

---

## Table of Contents

1. [Authentication](#authentication)
2. [Common Response Format](#common-response-format)
3. [Error Handling](#error-handling)
4. [API Endpoints](#api-endpoints)
   - [Authentication Endpoints](#authentication-endpoints)
   - [Account Endpoints](#account-endpoints)
   - [Profile Endpoints](#profile-endpoints)
   - [Dashboard Endpoints](#dashboard-endpoints)
   - [Analytics Endpoints](#analytics-endpoints)
   - [Transaction Endpoints](#transaction-endpoints)
   - [Payment Endpoints](#payment-endpoints)
   - [KYC Endpoints](#kyc-endpoints)
   - [Admin Endpoints](#admin-endpoints)
   - [Card Endpoints](#card-endpoints)
   - [Payroll Endpoints](#payroll-endpoints)
5. [Data Models](#data-models)
6. [Enums](#enums)
7. [Best Practices](#best-practices)
8. [Code Examples](#code-examples)

---

## Authentication

All protected endpoints require JWT Bearer token authentication. Include the token in the `Authorization` header:

```
Authorization: Bearer <your_jwt_token>
```

### Getting a Token

1. **Register** a new user (if needed)
2. **Login** with phone and password to receive access token and refresh token
3. **Use the access token** in subsequent requests
4. **Refresh the token** when it expires using the refresh token

---

## Common Response Format

All API responses follow this structure:

```typescript
interface ApiResponse<T> {
  success: boolean;
  data: T | null;
  message: string | null;
  code: string | null;
  errors: { [key: string]: string[] } | null;
}
```

### Success Response Example
```json
{
  "success": true,
  "data": { /* response data */ },
  "message": null,
  "code": null,
  "errors": null
}
```

### Error Response Example
```json
{
  "success": false,
  "data": null,
  "message": "Validation failed.",
  "code": "VALIDATION_FAILED",
  "errors": {
    "phone": ["Phone number is required"],
    "password": ["Password must be at least 8 characters"]
  }
}
```

---

## Error Handling

### HTTP Status Codes

- `200 OK` - Request successful
- `201 Created` - Resource created successfully
- `400 Bad Request` - Invalid request data
- `401 Unauthorized` - Missing or invalid token
- `403 Forbidden` - Insufficient permissions
- `404 Not Found` - Resource not found
- `500 Internal Server Error` - Server error

### Error Codes

Common error codes you may encounter:
- `VALIDATION_FAILED` - Request validation failed
- `UNAUTHORIZED` - Authentication required
- `FORBIDDEN` - Insufficient permissions
- `NOT_FOUND` - Resource not found
- `ACCOUNT_NOT_FOUND` - Account does not exist
- `INSUFFICIENT_BALANCE` - Not enough balance for transaction
- `TRANSACTION_FAILED` - Transaction processing failed

### Handling Errors in Frontend

```typescript
try {
  const response = await fetch(`${BASE_URL}/api/endpoint`, {
    headers: {
      'Authorization': `Bearer ${token}`,
      'Content-Type': 'application/json'
    }
  });
  
  const result = await response.json();
  
  if (!result.success) {
    // Handle error
    console.error('Error:', result.message);
    if (result.errors) {
      // Display validation errors
      Object.entries(result.errors).forEach(([field, messages]) => {
        console.error(`${field}: ${messages.join(', ')}`);
      });
    }
    return;
  }
  
  // Use result.data
  return result.data;
} catch (error) {
  console.error('Network error:', error);
}
```

---

## API Endpoints

### Authentication Endpoints

#### 1. Register User

**POST** `/auth/register`

**Content-Type:** `multipart/form-data`

**Request Body (Form Data):**
- `FullName` (string, required) - User's full name
- `Phone` (string, required) - Phone number
- `Password` (string, required) - Password
- `VerificationCode` (string, required) - OTP verification code
- `UserImage` (file, optional) - Profile image (max 5MB, jpeg/png/webp)
- `IcDocumentImage` (file, optional) - IC document image
- `IcDocumentJson` (string, optional) - IC document metadata as JSON
- `PassportDocumentImage` (file, optional) - Passport image
- `PassportDocumentJson` (string, optional) - Passport metadata as JSON
- `LivingIdentityDocumentImage` (file, optional) - Living identity document image
- `LivingIdentityDocumentJson` (string, optional) - Living identity metadata as JSON

**Response:** `ApiResponse<RegisterResponse>`

```typescript
interface RegisterResponse {
  id: number;
  fullName: string;
  phone: string;
  token: string;
  refreshToken: string;
}
```

**Example:**
```javascript
const formData = new FormData();
formData.append('FullName', 'John Doe');
formData.append('Phone', '+1234567890');
formData.append('Password', 'SecurePassword123');
formData.append('VerificationCode', '123456');
if (profileImage) {
  formData.append('UserImage', profileImage);
}

const response = await fetch(`${BASE_URL}/auth/register`, {
  method: 'POST',
  body: formData
});
```

---

#### 2. Login

**POST** `/auth/login`

**Request Body:**
```json
{
  "phone": "string",
  "password": "string"
}
```

**Response:** `ApiResponse<LoginResponse>`

```typescript
interface LoginResponse {
  id: number;
  fullName: string;
  phone: string;
  token: string;
  refreshToken: string;
}
```

---

#### 3. Refresh Token

**POST** `/auth/refresh`

**Request Body:**
```json
{
  "refreshToken": "string"
}
```

**Response:** `ApiResponse<LoginResponse>`

---

#### 4. Logout

**POST** `/auth/logout`

**Request Body:**
```json
{
  "refreshToken": "string"
}
```

**Response:** `ApiResponse<object>`

---

#### 5. Logout All Devices

**POST** `/auth/logoutAllDevices`

**Authorization:** Required (Bearer token)

**Response:** `ApiResponse<object>`

---

#### 6. Get Current User

**GET** `/auth/Me`

**Authorization:** Required (Bearer token)

**Response:** `ApiResponse<User>`

---

#### 7. Send Verification Code (OTP)

**POST** `/auth/send-verification`

**Request Body:**
```json
{
  "phone": "string"
}
```

**Response:** `ApiResponse<object>`

---

#### 8. Forgot Password

**POST** `/auth/forgot-password`

**Request Body:**
```json
{
  "phone": "string"
}
```

**Response:** `ApiResponse<object>`

---

#### 9. Reset Password

**POST** `/auth/reset-password`

**Request Body:**
```json
{
  "phone": "string",
  "verificationCode": "string",
  "newPassword": "string"
}
```

**Response:** `ApiResponse<object>`

---

### Account Endpoints

#### 1. Get My Account

**GET** `/api/accounts/me`

**Authorization:** Required (Bearer token)

**Response:** `ApiResponse<AccountResponse>`

```typescript
interface AccountResponse {
  id: number;
  accountNumber: string;
  balance: number;
  currency: string;
  status: AccountStatus;
  accountType: AccountType;
  createdAt: string; // ISO 8601 date-time
  updatedAt: string | null;
}
```

---

#### 2. Get My Cards

**GET** `/api/accounts/me/cards`

**Authorization:** Required (Bearer token)

**Response:** `ApiResponse<CardResponse[]>`

---

#### 3. Get Account by Number

**GET** `/api/accounts/by-number/{accountNumber}`

**Authorization:** Required (Bearer token)

**Path Parameters:**
- `accountNumber` (string, required)

**Response:** `ApiResponse<AccountResponse>`

---

### Profile Endpoints

#### 1. Get Profile

**GET** `/api/profile`

**Authorization:** Required (Bearer token)

**Response:** `ApiResponse<ProfileResponse>`

```typescript
interface ProfileResponse {
  id: number;
  fullName: string;
  phone: string;
  imageUrl: string | null;
  createdAt: string; // ISO 8601 date-time
  updatedAt: string | null;
}
```

---

#### 2. Update Profile

**PUT** `/api/profile`

**Authorization:** Required (Bearer token)

**Request Body:**
```json
{
  "fullName": "string (optional)",
  "phone": "string (optional)",
  "profileImageUrl": "string (optional)"
}
```

**Response:** `ApiResponse<ProfileResponse>`

---

### Dashboard Endpoints

#### 1. Get Dashboard

**GET** `/api/dashboard`

**Authorization:** Required (Bearer token)

**Response:** `ApiResponse<DashboardResponse>`

```typescript
interface DashboardResponse {
  totalBalance: number;
  totalDebit: number;
  totalCredit: number;
  user: UserInfoDto;
  cards: CardResponse[];
  recentTransactions: TransactionListItemDto[];
}

interface UserInfoDto {
  id: number;
  name: string;
  phone: string;
  accountCreatedAt: string; // ISO 8601 date-time
}

interface TransactionListItemDto {
  id: number;
  amount: number;
  direction: string; // "Credit" or "Debit"
  status: string;
  createdAt: string; // ISO 8601 date-time
}
```

---

### Analytics Endpoints

#### 1. Get Analytics Overview

**GET** `/api/analytics/overview`

**Authorization:** Required (Bearer token)

**Response:** `ApiResponse<AnalyticsOverviewResponse>`

```typescript
interface AnalyticsOverviewResponse {
  totalBalance: number;
  monthlyGrowthRate: number | null; // Percentage
}
```

---

#### 2. Get Transactions Analytics

**GET** `/api/analytics/transactions`

**Authorization:** Required (Bearer token)

**Query Parameters:**
- `startDate` (DateTime, optional) - ISO 8601 format
- `endDate` (DateTime, optional) - ISO 8601 format
- `direction` (string, optional) - "Credit" or "Debit"

**Response:** `ApiResponse<AnalyticsTransactionsResponse>`

```typescript
interface AnalyticsTransactionsResponse {
  totalCredit: number;
  totalDebit: number;
  startDate: string | null; // ISO 8601 date-time
  endDate: string | null; // ISO 8601 date-time
}
```

**Example:**
```
GET /api/analytics/transactions?startDate=2024-01-01T00:00:00Z&endDate=2024-12-31T23:59:59Z&direction=Credit
```

---

#### 3. Get Balance Trend

**GET** `/api/analytics/balance-trend`

**Authorization:** Required (Bearer token)

**Response:** `ApiResponse<AnalyticsBalanceTrendResponse>`

```typescript
interface AnalyticsBalanceTrendResponse {
  monthlyBalances: MonthlyBalanceDto[];
}

interface MonthlyBalanceDto {
  month: string; // Format: "YYYY-MM"
  balance: number;
}
```

---

#### 4. Get Transactions List (Paginated)

**GET** `/api/analytics/transactions-list`

**Authorization:** Required (Bearer token)

**Query Parameters:**
- `page` (int, default: 1)
- `pageSize` (int, default: 10)
- `startDate` (DateTime, optional) - ISO 8601 format
- `endDate` (DateTime, optional) - ISO 8601 format

**Response:** `ApiResponse<PaginatedListResponse<TransactionListItemResponse>>`

```typescript
interface PaginatedListResponse<T> {
  items: T[];
  totalCount: number;
  pageIndex: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

interface TransactionListItemResponse {
  id: number;
  amount: number;
  direction: string;
  status: string;
  createdAt: string; // ISO 8601 date-time
  // ... other transaction fields
}
```

---

### Transaction Endpoints

#### 1. Transfer Money

**POST** `/api/transactions/transfer`

**Authorization:** Required (Bearer token)

**Request Body:**
```json
{
  "toAccountNumber": "string",
  "amount": 0,
  "currency": "string",
  "description": "string (optional)"
}
```

**Response:** `ApiResponse<TransactionResponse>`

---

#### 2. Get Transactions by Account

**GET** `/api/transactions/account/{accountId}`

**Authorization:** Required (Bearer token)

**Path Parameters:**
- `accountId` (int, required)

**Query Parameters:**
- `pageNumber` (int, default: 1)
- `pageSize` (int, default: 10)

**Response:** `ApiResponse<PaginatedListResponse<TransactionResponse>>`

---

#### 3. Get Transaction by ID

**GET** `/api/transactions/{id}`

**Authorization:** Required (Bearer token)

**Path Parameters:**
- `id` (int, required)

**Response:** `ApiResponse<TransactionResponse>`

---

#### 4. Retry Transaction

**POST** `/api/transactions/{transactionId}/retry`

**Authorization:** Required (Bearer token)

**Path Parameters:**
- `transactionId` (int, required)

**Response:** `ApiResponse<TransactionResponse>`

---

### Payment Endpoints

#### 1. Generate QR Code for Payment

**GET** `/api/payments/qr/{accountNumber}`

**Authorization:** Required (Bearer token)

**Path Parameters:**
- `accountNumber` (string, required)

**Response:** `ApiResponse<QRCodeResponse>`

---

#### 2. Initiate Payment

**POST** `/api/payments/initiate`

**Authorization:** Required (Bearer token)

**Request Body:**
```json
{
  "toAccountNumber": "string",
  "amount": 0,
  "currency": "string",
  "description": "string (optional)"
}
```

**Response:** `ApiResponse<PaymentResponse>`

---

#### 3. Complete Payment

**POST** `/api/payments/pay`

**Authorization:** Required (Bearer token)

**Request Body:**
```json
{
  "paymentId": "string",
  "verificationCode": "string"
}
```

**Response:** `ApiResponse<PaymentResponse>`

---

### KYC Endpoints

#### 1. Upload IC Document

**POST** `/api/kyc/ic`

**Authorization:** Required (Bearer token)

**Request Body:** (multipart/form-data or JSON with image URL)
```json
{
  "documentNumber": "string",
  "issueDate": "string (ISO 8601)",
  "expiryDate": "string (ISO 8601)",
  "imageUrl": "string"
}
```

**Response:** `ApiResponse<KycDocumentResponse>`

---

#### 2. Upload Passport Document

**POST** `/api/kyc/passport`

**Authorization:** Required (Bearer token)

**Request Body:**
```json
{
  "documentNumber": "string",
  "issueDate": "string (ISO 8601)",
  "expiryDate": "string (ISO 8601)",
  "imageUrl": "string"
}
```

**Response:** `ApiResponse<KycDocumentResponse>`

---

#### 3. Upload Living Identity Document

**POST** `/api/kyc/living-identity`

**Authorization:** Required (Bearer token)

**Request Body:**
```json
{
  "documentNumber": "string",
  "issueDate": "string (ISO 8601)",
  "expiryDate": "string (ISO 8601)",
  "imageUrl": "string"
}
```

**Response:** `ApiResponse<KycDocumentResponse>`

---

#### 4. Get KYC Status

**GET** `/api/kyc/status`

**Authorization:** Required (Bearer token)

**Response:** `ApiResponse<KycStatusResponse>`

---

### Admin Endpoints

**Note:** All admin endpoints require `Admin` role and Bearer token authentication.

#### 1. List Users

**GET** `/api/admin/users`

**Authorization:** Required (Admin role)

**Query Parameters:**
- `approvalStatus` (ApprovalStatus, optional)
- `pageNumber` (int, default: 1)
- `pageSize` (int, default: 10)

**Response:** `ApiResponse<PaginatedListResponse<UserApprovalListItemResponse>>`

```typescript
interface UserApprovalListItemResponse {
  id: number;
  fullName: string | null;
  phone: string | null;
  kycStatus: KycStatus;
  kybStatus: KybStatus;
  approvalStatus: ApprovalStatus;
  accountId: number | null;
  accountNumber: string | null;
  accountStatus: AccountStatus;
}
```

---

#### 2. Get User by ID

**GET** `/api/admin/users/{userId}`

**Authorization:** Required (Admin role)

**Path Parameters:**
- `userId` (int, required)

**Response:** `ApiResponse<UserApprovalListItemResponse>`

---

#### 3. Approve or Reject User

**POST** `/api/admin/users/{userId}/approval`

**Authorization:** Required (Admin role)

**Path Parameters:**
- `userId` (int, required)

**Request Body:**
```json
{
  "approved": true,
  "reason": "string (optional)"
}
```

**Response:** `ApiResponse<object>`

---

#### 4. Set Account Status

**PUT** `/api/admin/accounts/{accountId}/status`

**Authorization:** Required (Admin role)

**Path Parameters:**
- `accountId` (int, required)

**Request Body:**
```json
{
  "status": "Active" // AccountStatus enum
}
```

**Response:** `ApiResponse<object>`

---

#### 5. Verify IC Document

**PUT** `/api/admin/kyc/ic/{documentId}/verify`

**Authorization:** Required (Admin role)

**Path Parameters:**
- `documentId` (int, required)

**Request Body:**
```json
{
  "status": "Approved" // KycDocumentStatus enum
}
```

**Response:** `ApiResponse<object>`

---

#### 6. Verify Passport Document

**PUT** `/api/admin/kyc/passport/{documentId}/verify`

**Authorization:** Required (Admin role)

**Path Parameters:**
- `documentId` (int, required)

**Request Body:**
```json
{
  "status": "Approved" // KycDocumentStatus enum
}
```

**Response:** `ApiResponse<object>`

---

#### 7. Verify Living Identity Document

**PUT** `/api/admin/kyc/living-identity/{documentId}/verify`

**Authorization:** Required (Admin role)

**Path Parameters:**
- `documentId` (int, required)

**Request Body:**
```json
{
  "status": "Approved" // KycDocumentStatus enum
}
```

**Response:** `ApiResponse<object>`

---

#### 8. Get Admin Dashboard Metrics

**GET** `/api/admin/dashboard/metrics`

**Authorization:** Required (Admin role)

**Response:** `ApiResponse<AdminDashboardMetricsResponse>`

```typescript
interface AdminDashboardMetricsResponse {
  activeUsersCount: number;
  pendingUsersCount: number;
  rejectedUsersCount: number;
  totalUsersCount: number;
}
```

---

#### 9. Get Admin Dashboard Users

**GET** `/api/admin/dashboard/users`

**Authorization:** Required (Admin role)

**Query Parameters:**
- `search` (string, optional) - Search by name or phone
- `status` (string, optional) - Filter by status
- `page` (int, default: 1)
- `pageSize` (int, default: 10)

**Response:** `ApiResponse<PaginatedListResponse<UserListItemResponse>>`

```typescript
interface UserListItemResponse {
  id: number;
  fullName: string;
  phone: string;
  role: UserRole;
  approvalStatus: ApprovalStatus;
  kycStatus: KycStatus;
  createdAt: string; // ISO 8601 date-time
}
```

---

#### 10. Get User Status

**GET** `/api/admin/dashboard/users/{userId}/status`

**Authorization:** Required (Admin role)

**Path Parameters:**
- `userId` (int, required)

**Response:** `ApiResponse<UserStatusResponse>`

```typescript
interface UserStatusResponse {
  userId: number;
  approvalStatus: ApprovalStatus;
  kycStatus: KycStatus;
  kybStatus: KybStatus;
  accountStatus: AccountStatus | null;
  statusDescription: string;
}
```

---

#### 11. Review Transaction Risk

**GET** `/api/admin/transactions/risk-review`

**Authorization:** Required (Admin role)

**Query Parameters:**
- `pageNumber` (int, default: 1)
- `pageSize` (int, default: 10)

**Response:** `ApiResponse<PaginatedListResponse<TransactionResponse>>`

---

#### 12. Review Transaction

**POST** `/api/admin/transactions/{transactionId}/review`

**Authorization:** Required (Admin role)

**Path Parameters:**
- `transactionId` (int, required)

**Request Body:**
```json
{
  "approved": true,
  "reason": "string (optional)"
}
```

**Response:** `ApiResponse<object>`

---

#### 13. List All Cards (Admin)

**GET** `/api/admin/cards`

**Authorization:** Required (Admin role)

**Query Parameters:**
- `pageNumber` (int, default: 1)
- `pageSize` (int, default: 10)

**Response:** `ApiResponse<PaginatedListResponse<CardResponse>>`

---

#### 14. Get Cards by Account (Admin)

**GET** `/api/admin/cards/account/{accountId}`

**Authorization:** Required (Admin role)

**Path Parameters:**
- `accountId` (int, required)

**Response:** `ApiResponse<CardResponse[]>`

---

#### 15. Revoke Card

**PUT** `/api/admin/cards/{cardId}/revoke`

**Authorization:** Required (Admin role)

**Path Parameters:**
- `cardId` (int, required)

**Response:** `ApiResponse<object>`

---

### Payroll Endpoints

#### 1. Create Payroll Job

**POST** `/api/admin/payroll`

**Authorization:** Required (Admin role)

**Request Body:**
```json
{
  "employeeAccountNumber": "string",
  "amount": 0,
  "currency": "string",
  "schedule": "string (cron expression)",
  "nextRunAt": "string (ISO 8601 date-time)"
}
```

**Response:** `ApiResponse<PayrollJobResponse>`

---

#### 2. Update Payroll Job

**PUT** `/api/admin/payroll/{jobId}`

**Authorization:** Required (Admin role)

**Path Parameters:**
- `jobId` (int, required)

**Request Body:**
```json
{
  "amount": 0,
  "currency": "string",
  "schedule": "string",
  "nextRunAt": "string (ISO 8601 date-time)",
  "status": "Active" // PayrollJobStatus enum
}
```

**Response:** `ApiResponse<PayrollJobResponse>`

---

## Data Models

### Common Models

#### ApiResponse<T>
```typescript
interface ApiResponse<T> {
  success: boolean;
  data: T | null;
  message: string | null;
  code: string | null;
  errors: { [key: string]: string[] } | null;
}
```

#### PaginatedListResponse<T>
```typescript
interface PaginatedListResponse<T> {
  items: T[];
  totalCount: number;
  pageIndex: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}
```

#### AccountResponse
```typescript
interface AccountResponse {
  id: number;
  accountNumber: string;
  balance: number;
  currency: string;
  status: AccountStatus;
  accountType: AccountType;
  createdAt: string; // ISO 8601
  updatedAt: string | null;
}
```

#### CardResponse
```typescript
interface CardResponse {
  id: number;
  cardNumber: string; // Masked
  expiryDate: string; // ISO 8601
  cvv: string; // Masked
  status: CardStatus;
  accountId: number;
  createdAt: string; // ISO 8601
}
```

#### TransactionResponse
```typescript
interface TransactionResponse {
  id: number;
  amount: number;
  currency: string;
  direction: TransactionDirection;
  status: TransactionStatus;
  type: TransactionType;
  description: string | null;
  fromAccountId: number | null;
  toAccountId: number | null;
  createdAt: string; // ISO 8601
  updatedAt: string | null;
}
```

---

## Enums

### AccountStatus
```typescript
enum AccountStatus {
  Active = 0,
  Suspended = 1,
  Closed = 2
}
```

### AccountType
```typescript
enum AccountType {
  Savings = 0,
  Current = 1,
  Business = 2
}
```

### ApprovalStatus
```typescript
enum ApprovalStatus {
  Pending = 0,
  Approved = 1,
  Rejected = 2
}
```

### KycStatus
```typescript
enum KycStatus {
  NotStarted = 0,
  Pending = 1,
  Approved = 2,
  Rejected = 3
}
```

### KybStatus
```typescript
enum KybStatus {
  NotStarted = 0,
  Pending = 1,
  Approved = 2,
  Rejected = 3
}
```

### KycDocumentStatus
```typescript
enum KycDocumentStatus {
  Pending = 0,
  Approved = 1,
  Rejected = 2
}
```

### TransactionStatus
```typescript
enum TransactionStatus {
  Pending = 0,
  Processing = 1,
  Completed = 2,
  Failed = 3,
  Cancelled = 4,
  Reversed = 5
}
```

### TransactionDirection
```typescript
enum TransactionDirection {
  Credit = 0,
  Debit = 1
}
```

### TransactionType
```typescript
enum TransactionType {
  Transfer = 0,
  Payment = 1
}
```

### UserRole
```typescript
enum UserRole {
  Customer = 1,
  Admin = 2
}
```

### CardStatus
```typescript
enum CardStatus {
  Active = 0,
  Suspended = 1,
  Revoked = 2
}
```

### PayrollJobStatus
```typescript
enum PayrollJobStatus {
  Active = 0,
  Paused = 1,
  Completed = 2,
  Cancelled = 3
}
```

---

## Best Practices

### 1. Token Management

- Store tokens securely (use secure storage, not localStorage for production)
- Implement token refresh before expiration
- Handle 401 errors by redirecting to login
- Clear tokens on logout

```typescript
// Example token refresh logic
async function refreshTokenIfNeeded() {
  const token = getStoredToken();
  const refreshToken = getStoredRefreshToken();
  
  if (isTokenExpired(token) && refreshToken) {
    const response = await fetch(`${BASE_URL}/auth/refresh`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ refreshToken })
    });
    
    const result = await response.json();
    if (result.success) {
      storeToken(result.data.token);
      storeRefreshToken(result.data.refreshToken);
      return result.data.token;
    }
  }
  
  return token;
}
```

### 2. Error Handling

- Always check `response.success` before using `response.data`
- Display user-friendly error messages
- Handle validation errors by showing field-specific messages
- Implement retry logic for network errors

### 3. Date/Time Handling

- All dates are in ISO 8601 format (e.g., "2024-01-15T10:30:00Z")
- Use UTC for date queries
- Format dates for display in user's local timezone

### 4. File Uploads

- Validate file size (max 5MB for images)
- Validate file type (jpeg, png, webp for images)
- Show upload progress
- Handle upload errors gracefully

### 5. Pagination

- Always implement pagination for list endpoints
- Show page numbers and total count
- Implement "Load More" or traditional pagination
- Handle empty states

### 6. Loading States

- Show loading indicators during API calls
- Disable buttons during submission
- Prevent duplicate submissions

### 7. Security

- Never expose tokens in URLs or logs
- Use HTTPS only
- Validate all user inputs
- Sanitize data before display

---

## Code Examples

### TypeScript/JavaScript API Client Example

```typescript
const BASE_URL = 'https://kzcajtqhoz.a.pinggy.link';

class SuperMemoAPI {
  private token: string | null = null;
  private refreshToken: string | null = null;

  setTokens(token: string, refreshToken: string) {
    this.token = token;
    this.refreshToken = refreshToken;
  }

  private async request<T>(
    endpoint: string,
    options: RequestInit = {}
  ): Promise<ApiResponse<T>> {
    const url = `${BASE_URL}${endpoint}`;
    const headers: HeadersInit = {
      'Content-Type': 'application/json',
      ...options.headers,
    };

    if (this.token) {
      headers['Authorization'] = `Bearer ${this.token}`;
    }

    try {
      const response = await fetch(url, {
        ...options,
        headers,
      });

      const data: ApiResponse<T> = await response.json();

      if (!data.success && response.status === 401) {
        // Handle token refresh or redirect to login
        await this.handleUnauthorized();
      }

      return data;
    } catch (error) {
      throw new Error(`Network error: ${error}`);
    }
  }

  private async handleUnauthorized() {
    // Implement token refresh or redirect to login
  }

  // Authentication
  async login(phone: string, password: string) {
    const response = await this.request<LoginResponse>('/auth/login', {
      method: 'POST',
      body: JSON.stringify({ phone, password }),
    });

    if (response.success && response.data) {
      this.setTokens(response.data.token, response.data.refreshToken);
    }

    return response;
  }

  async register(formData: FormData) {
    const response = await fetch(`${BASE_URL}/auth/register`, {
      method: 'POST',
      body: formData,
    });

    const data: ApiResponse<RegisterResponse> = await response.json();

    if (data.success && data.data) {
      this.setTokens(data.data.token, data.data.refreshToken);
    }

    return data;
  }

  // Profile
  async getProfile() {
    return this.request<ProfileResponse>('/api/profile');
  }

  async updateProfile(updates: UpdateProfileRequest) {
    return this.request<ProfileResponse>('/api/profile', {
      method: 'PUT',
      body: JSON.stringify(updates),
    });
  }

  // Dashboard
  async getDashboard() {
    return this.request<DashboardResponse>('/api/dashboard');
  }

  // Analytics
  async getAnalyticsOverview() {
    return this.request<AnalyticsOverviewResponse>('/api/analytics/overview');
  }

  async getAnalyticsTransactions(
    startDate?: string,
    endDate?: string,
    direction?: string
  ) {
    const params = new URLSearchParams();
    if (startDate) params.append('startDate', startDate);
    if (endDate) params.append('endDate', endDate);
    if (direction) params.append('direction', direction);

    return this.request<AnalyticsTransactionsResponse>(
      `/api/analytics/transactions?${params.toString()}`
    );
  }

  async getBalanceTrend() {
    return this.request<AnalyticsBalanceTrendResponse>(
      '/api/analytics/balance-trend'
    );
  }

  async getTransactionsList(
    page: number = 1,
    pageSize: number = 10,
    startDate?: string,
    endDate?: string
  ) {
    const params = new URLSearchParams({
      page: page.toString(),
      pageSize: pageSize.toString(),
    });
    if (startDate) params.append('startDate', startDate);
    if (endDate) params.append('endDate', endDate);

    return this.request<
      PaginatedListResponse<TransactionListItemResponse>
    >(`/api/analytics/transactions-list?${params.toString()}`);
  }

  // Transactions
  async transfer(
    toAccountNumber: string,
    amount: number,
    currency: string,
    description?: string
  ) {
    return this.request<TransactionResponse>('/api/transactions/transfer', {
      method: 'POST',
      body: JSON.stringify({
        toAccountNumber,
        amount,
        currency,
        description,
      }),
    });
  }

  // Accounts
  async getMyAccount() {
    return this.request<AccountResponse>('/api/accounts/me');
  }

  async getMyCards() {
    return this.request<CardResponse[]>('/api/accounts/me/cards');
  }
}

// Usage
const api = new SuperMemoAPI();

// Login
const loginResult = await api.login('+1234567890', 'password123');
if (loginResult.success) {
  console.log('Logged in:', loginResult.data);
}

// Get Dashboard
const dashboardResult = await api.getDashboard();
if (dashboardResult.success) {
  console.log('Dashboard:', dashboardResult.data);
}
```

### React Hook Example

```typescript
import { useState, useEffect } from 'react';

function useDashboard() {
  const [data, setData] = useState<DashboardResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    async function fetchDashboard() {
      try {
        setLoading(true);
        const result = await api.getDashboard();
        
        if (result.success && result.data) {
          setData(result.data);
        } else {
          setError(result.message || 'Failed to load dashboard');
        }
      } catch (err) {
        setError('Network error');
      } finally {
        setLoading(false);
      }
    }

    fetchDashboard();
  }, []);

  return { data, loading, error };
}
```

---

## Additional Resources

- **Swagger UI:** https://kzcajtqhoz.a.pinggy.link/swagger/index.html
- **Base URL:** https://kzcajtqhoz.a.pinggy.link
- **API Version:** v1

---

## Support

For API-related questions or issues, refer to the Swagger documentation or contact the backend team.

**Last Updated:** February 6, 2025
