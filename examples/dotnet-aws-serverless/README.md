# Contact Manager - .NET AWS Serverless Application

A serverless contact management application built with .NET 9, AWS Lambda, and DynamoDB, following Test-Driven Development (TDD) and Domain-Driven Design (DDD) principles.

## Architecture

This application follows a clean architecture with three main layers:

- **ContactManager.Core**: Domain entities, business logic, DTOs, and validation
- **ContactManager.Adapters**: Data access implementations (DynamoDB repository)
- **ContactManager.Api**: AWS Lambda function with HTTP API endpoints

## Features

- **Contact Registration**: Store contact information (name and email) with validation
- **DynamoDB Storage**: Uses single table design with low-level DynamoDB API
- **Native AOT**: Optimized for cold start performance
- **Structured Logging**: AWS Lambda Powertools for observability
- **Input Validation**: FluentValidation for robust input checking
- **Error Handling**: Result pattern for functional error handling

## API Endpoints

### POST /contacts

Creates a new contact.

**Request Body:**
```json
{
  "name": "John Doe",
  "email": "john.doe@example.com"
}
```

**Response (201 Created):**
```json
{
  "id": "123e4567-e89b-12d3-a456-426614174000",
  "name": "John Doe", 
  "email": "john.doe@example.com",
  "createdAt": "2024-06-26T10:00:00.000Z"
}
```

**Error Responses:**
- `400 Bad Request`: Validation failures
- `409 Conflict`: Contact already exists
- `500 Internal Server Error`: Server errors

## Data Model

### DynamoDB Table Design

Single table design using partition key (PK) and sort key (SK):

- **PK**: `CONTACT#{ContactId}`
- **SK**: `CONTACT#{ContactId}`
- **Attributes**: Id, Name, Email, CreatedAt, TTL

### Domain Entities

- **ContactId**: Strongly-typed ID value object
- **Contact**: Domain entity with validation and behavior
- **ContactRequest**: Input DTO with FluentValidation
- **ContactResponse**: Output DTO for API responses

## Development

### Prerequisites

- .NET 9 SDK
- AWS CLI configured
- SAM CLI
- Docker (for local DynamoDB)

### Build

```bash
dotnet build
```

### Test

```bash
dotnet test
```

### Local Development

```bash
# Start local DynamoDB
sam local start-api

# Deploy to AWS
sam build
sam deploy --guided
```

### Environment Variables

- `ContactsTableName`: DynamoDB table name
- `POWERTOOLS_LOG_LEVEL`: Logging level (INFO, DEBUG, etc.)
- `POWERTOOLS_SERVICE_NAME`: Service name for structured logging

## Deployment

The application uses AWS SAM for infrastructure as code:

```bash
# Build the application
sam build

# Deploy to AWS
sam deploy

# Deploy to specific environment
sam deploy --parameter-overrides Environment=staging
```

## Infrastructure

- **AWS Lambda**: Serverless compute with Native AOT
- **Amazon DynamoDB**: NoSQL database with single table design
- **Amazon API Gateway**: HTTP API with CORS support
- **CloudWatch Logs**: Centralized logging with 14-day retention

## Monitoring

- **Structured Logging**: JSON logs with correlation IDs
- **AWS X-Ray**: Distributed tracing (can be enabled)
- **CloudWatch Metrics**: Lambda and DynamoDB metrics
- **Lambda Powertools**: Enhanced observability

## Security

- **Input Validation**: Comprehensive validation using FluentValidation
- **Error Handling**: No sensitive data in error responses
- **IAM Policies**: Least privilege access to DynamoDB
- **HTTPS Only**: All API communication over HTTPS

## Best Practices Implemented

- **Test-Driven Development**: All code written with failing tests first
- **Domain-Driven Design**: Clear separation of concerns
- **SOLID Principles**: Clean, maintainable code architecture
- **Immutable Data**: Records and value objects for data integrity
- **Result Pattern**: Functional error handling without exceptions
- **Native AOT**: Optimized for serverless cold start performance
- **Structured Logging**: Observable and debuggable applications