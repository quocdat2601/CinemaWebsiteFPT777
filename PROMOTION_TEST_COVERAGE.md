# Promotion Test Coverage Documentation

## Overview
This document outlines the comprehensive test coverage implemented for all promotion-related code in the MovieTheater application. The coverage was expanded to address SonarQube findings and ensure robust testing of all promotion functionalities.

## Test Coverage Summary

### Files Covered:
1. **Controllers/PromotionController.cs** - Comprehensive coverage of all action methods including previously uncovered lines
2. **Service/PromotionService.cs** - Full coverage of business logic including complex eligibility checks
3. **Repository/PromotionRepository.cs** - Complete CRUD operations coverage
4. **Models/Promotion.cs, PromotionCondition.cs, ConditionType.cs** - Property and collection tests
5. **ViewModels/PromotionViewModel.cs** - Validation and property handling tests

### Specific Test Cases Added:

#### PromotionController Tests:
- **Basic CRUD Operations**: List, Index, Create (GET/POST), Edit (GET/POST), Delete (GET/POST)
- **Image File Handling**: Size validation, extension validation, empty files, path traversal prevention
- **Error Handling**: Exception scenarios, invalid model states, file validation errors
- **SignalR Integration**: Dashboard hub notifications
- **Security**: Path traversal prevention, file type validation
- **Previously Uncovered Lines Coverage**:
  - **Lines 91-117**: Date column handling logic in Create method (dateColumns array check)
  - **Lines 143-166**: Path traversal validation in Create method (filePath.StartsWith check)
  - **Lines 206-213**: Mapping first PromotionCondition to ViewModel in Edit GET method
  - **Lines 218-224**: ID mismatch validation in Edit POST method
  - **Lines 322-352**: Complex condition handling logic in Edit POST method (existing vs new conditions, date handling)

#### PromotionService Tests:
- **CRUD Operations**: GetAll, GetById, Add, Update, Delete, Save
- **Business Logic**: GetBestEligiblePromotionForBooking, IsPromotionEligible
- **Food Promotions**: GetEligibleFoodPromotions, ApplyFoodPromotionsToFoods
- **Condition Types**: seat, typename, moviename, showdate, pricepercent, accountid
- **Operators**: >, <, >=, <=, =, !=
- **Edge Cases**: null/empty inputs, invalid values, unknown fields, negative/zero/null discount levels

#### PromotionRepository Tests:
- **CRUD Operations**: GetAll, GetById, Add, Update, Delete, Save
- **Complex Updates**: Handling existing and new PromotionCondition entities
- **Database Interactions**: Mocking DbContext and DbSet operations

#### Model Tests:
- **Promotion Model**: Property setting/getting, default values, collection initialization
- **PromotionCondition Model**: Property validation, null handling
- **ConditionType Model**: Basic property tests

#### ViewModel Tests:
- **PromotionViewModel**: Property validation, default values, validation attributes
- **Edge Cases**: null/empty/whitespace/long strings, special characters
- **Validation Attributes**: Required, Display, Range attributes

## How to Run Tests

### Basic Test Execution:
```bash
dotnet test
```

### With Code Coverage:
```bash
dotnet test --collect:"XPlat Code Coverage" --results-directory coverage-report
```

### Using Batch Script:
```bash
run-promotion-tests.bat
```

## Test Categories

### 1. Edge Cases Covered:
- **Null/Empty Inputs**: Handling of null or empty strings, collections, and objects
- **Invalid Values**: Negative numbers, zero values, invalid file types
- **Boundary Values**: Maximum file sizes, date ranges, numeric limits
- **Special Characters**: Unicode characters, special symbols in strings

### 2. Business Logic Coverage:
- **Promotion Eligibility**: Complex conditional logic for determining eligible promotions
- **Discount Calculations**: Various discount level scenarios and calculations
- **Condition Matching**: Different condition types and operator combinations
- **Date Handling**: Date column detection and value formatting

### 3. Security Aspects:
- **Path Traversal Prevention**: File path validation to prevent directory traversal attacks
- **File Type Validation**: Strict validation of allowed file extensions
- **File Size Limits**: Prevention of large file uploads
- **Input Sanitization**: Proper handling of user inputs

### 4. Integration Points:
- **SignalR Hub**: Dashboard notifications for real-time updates
- **File System**: Image upload, deletion, and directory management
- **Database**: Entity Framework operations and transaction handling
- **Model Binding**: ViewModel validation and model state handling

## Coverage Metrics

The test suite provides comprehensive coverage for:
- **Line Coverage**: All critical code paths are exercised
- **Branch Coverage**: Both true and false conditions are tested
- **Exception Handling**: Error scenarios and exception flows
- **Integration Points**: External service and system interactions

## Maintenance Notes

### Adding New Tests:
1. Follow the existing naming convention: `MethodName_Scenario_ExpectedResult`
2. Use appropriate mocking for external dependencies
3. Test both positive and negative scenarios
4. Include edge cases and boundary conditions

### Updating Tests:
1. Ensure tests remain focused on single responsibilities
2. Update mocks when service interfaces change
3. Maintain test data consistency across related tests
4. Document any complex test scenarios

## Files Created/Modified

### New Test Files:
- `MovieTheater.Tests/Repository/PromotionRepositoryTests.cs`
- `MovieTheater.Tests/Models/PromotionModelTests.cs`
- `MovieTheater.Tests/ViewModels/PromotionViewModelTests.cs`

### Enhanced Test Files:
- `MovieTheater.Tests/Controller/PromotionControllerTests.cs` (significantly expanded)
- `MovieTheater.Tests/Service/PromotionServiceTests.cs` (extensively enhanced)

### Supporting Files:
- `run-promotion-tests.bat` - Test execution script
- `PROMOTION_TEST_COVERAGE.md` - This documentation file

## Conclusion

The promotion test coverage has been significantly enhanced to address SonarQube findings and ensure comprehensive testing of all promotion-related functionality. The test suite now covers:

- All controller actions with various scenarios
- Complex business logic with multiple condition types
- Repository layer with database interactions
- Model validation and property handling
- Security aspects including file upload validation
- Integration points with external systems

This comprehensive coverage ensures the reliability and maintainability of the promotion system while providing confidence in the codebase quality. 