# 🤝 Contributing to Cinema Website FPT777

Thank you for your interest in contributing to our Cinema Website project! This document provides guidelines and information for contributors.

## 📋 Table of Contents

- [Code of Conduct](#code-of-conduct)
- [Getting Started](#getting-started)
- [Development Setup](#development-setup)
- [Coding Standards](#coding-standards)
- [Testing Guidelines](#testing-guidelines)
- [Pull Request Process](#pull-request-process)
- [Issue Reporting](#issue-reporting)
- [Communication](#communication)

## 📜 Code of Conduct

This project and everyone participating in it is governed by our Code of Conduct. By participating, you are expected to uphold this code.

### Our Standards

- **Be respectful** and inclusive of all contributors
- **Be collaborative** and open to different viewpoints
- **Be constructive** in feedback and criticism
- **Be professional** in all interactions

## 🚀 Getting Started

### Prerequisites

- **.NET 8.0 SDK** or later
- **SQL Server 2019** or later
- **Git** for version control
- **Visual Studio 2022** or **VS Code**

### Fork and Clone

1. **Fork the repository** on GitHub
2. **Clone your fork** locally:
   ```bash
   git clone https://github.com/YOUR_USERNAME/CinemaWebsiteFPT777.git
   cd CinemaWebsiteFPT777
   ```
3. **Add the upstream remote**:
   ```bash
   git remote add upstream https://github.com/quocdat2601/CinemaWebsiteFPT777.git
   ```

## 🛠️ Development Setup

### 1. Database Setup

```bash
# Restore the database
sqlcmd -S (local) -i Cinama.sql
```

### 2. Configuration

Update `appsettings.json` with your local settings:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(local);uid=sa;pwd=your_password;database=MovieTheater;TrustServerCertificate=True;"
  },
  "Authentication": {
    "Google": {
      "ClientId": "your_google_client_id",
      "ClientSecret": "your_google_client_secret"
    }
  }
}
```

### 3. Install Dependencies

```bash
dotnet restore
```

### 4. Run the Application

```bash
dotnet run
```

## 📝 Coding Standards

### C# Coding Conventions

- Follow **Microsoft C# Coding Conventions**
- Use **PascalCase** for public members and types
- Use **camelCase** for private fields and local variables
- Use **UPPER_CASE** for constants
- Prefer **var** for local variable declarations when type is obvious

### Naming Conventions

```csharp
// ✅ Good
public class MovieService : IMovieService
{
    private readonly IMovieRepository _movieRepository;
    
    public async Task<Movie> GetMovieByIdAsync(string id)
    {
        // Implementation
    }
}

// ❌ Avoid
public class movieService : ImovieService
{
    private readonly IMovieRepository movieRepository;
    
    public async Task<Movie> getMovieByIdAsync(string id)
    {
        // Implementation
    }
}
```

### File Organization

- **One class per file** with matching filename
- **Group related classes** in appropriate folders
- **Use partial classes** for large classes when appropriate
- **Keep files under 500 lines** when possible

### Error Handling

```csharp
// ✅ Good - Use specific exception types
public async Task<Movie> GetMovieAsync(string id)
{
    if (string.IsNullOrEmpty(id))
        throw new ArgumentException("Movie ID cannot be null or empty", nameof(id));
    
    var movie = await _movieRepository.GetByIdAsync(id);
    if (movie == null)
        throw new NotFoundException($"Movie with ID {id} not found");
    
    return movie;
}

// ❌ Avoid - Generic exception handling
public async Task<Movie> GetMovieAsync(string id)
{
    try
    {
        return await _movieRepository.GetByIdAsync(id);
    }
    catch (Exception ex)
    {
        // Too generic
        throw;
    }
}
```

## 🧪 Testing Guidelines

### Test Structure

- **Unit Tests**: Test individual methods and classes
- **Integration Tests**: Test component interactions
- **End-to-End Tests**: Test complete user workflows

### Test Naming Convention

```csharp
[Fact]
public void GetMovieById_WithValidId_ReturnsMovie()
{
    // Arrange
    var movieId = "MOV001";
    var expectedMovie = new Movie { Id = movieId, Name = "Test Movie" };
    _mockRepository.Setup(r => r.GetByIdAsync(movieId))
                  .ReturnsAsync(expectedMovie);
    
    // Act
    var result = _service.GetMovieByIdAsync(movieId).Result;
    
    // Assert
    Assert.Equal(expectedMovie, result);
}

[Fact]
public void GetMovieById_WithInvalidId_ThrowsArgumentException()
{
    // Arrange
    var invalidId = "";
    
    // Act & Assert
    var exception = Assert.ThrowsAsync<ArgumentException>(
        () => _service.GetMovieByIdAsync(invalidId));
    Assert.Contains("Movie ID cannot be null or empty", exception.Result.Message);
}
```

### Running Tests

```bash
# Run all tests
dotnet test

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test project
dotnet test MovieTheater.Tests/

# Run tests in watch mode
dotnet watch test
```

## 🔄 Pull Request Process

### 1. Create a Feature Branch

```bash
git checkout -b feature/your-feature-name
# or
git checkout -b bugfix/issue-description
```

### 2. Make Your Changes

- **Write clear, descriptive commit messages**
- **Make small, focused commits**
- **Follow the coding standards**

### 3. Test Your Changes

```bash
# Run all tests
dotnet test

# Build the project
dotnet build

# Run the application locally
dotnet run
```

### 4. Update Documentation

- **Update README.md** if adding new features
- **Add inline documentation** for complex code
- **Update API documentation** if changing endpoints

### 5. Submit Pull Request

1. **Push your branch** to your fork
2. **Create a Pull Request** against the main branch
3. **Fill out the PR template** completely
4. **Link related issues** if applicable

### Pull Request Template

```markdown
## Description
Brief description of changes made

## Type of Change
- [ ] Bug fix (non-breaking change which fixes an issue)
- [ ] New feature (non-breaking change which adds functionality)
- [ ] Breaking change (fix or feature that would cause existing functionality to not work as expected)
- [ ] Documentation update

## Testing
- [ ] Unit tests pass
- [ ] Integration tests pass
- [ ] Manual testing completed
- [ ] Code coverage maintained or improved

## Checklist
- [ ] Code follows the project's coding standards
- [ ] Self-review completed
- [ ] Code is commented, particularly in hard-to-understand areas
- [ ] Corresponding changes to documentation made
- [ ] No new warnings generated
- [ ] Tests added for new functionality

## Screenshots (if applicable)
Add screenshots for UI changes

## Additional Notes
Any additional information or context
```

## 🐛 Issue Reporting

### Before Creating an Issue

1. **Check existing issues** for duplicates
2. **Search the documentation** for solutions
3. **Try to reproduce** the issue locally

### Issue Template

```markdown
## Bug Description
Clear and concise description of the bug

## Steps to Reproduce
1. Go to '...'
2. Click on '...'
3. Scroll down to '...'
4. See error

## Expected Behavior
What you expected to happen

## Actual Behavior
What actually happened

## Environment
- OS: [e.g., Windows 11, macOS 14.0]
- Browser: [e.g., Chrome 120, Firefox 121]
- .NET Version: [e.g., 8.0.0]
- Database: [e.g., SQL Server 2019]

## Additional Context
Any other context, logs, or screenshots
```

## 💬 Communication

### Communication Channels

- **GitHub Issues**: For bug reports and feature requests
- **GitHub Discussions**: For general questions and discussions
- **Pull Request Comments**: For code review feedback

### Best Practices

- **Be respectful** and constructive
- **Provide context** when asking questions
- **Use clear language** and avoid jargon
- **Be patient** with responses

## 📚 Additional Resources

### Documentation

- [ASP.NET Core Documentation](https://docs.microsoft.com/en-us/aspnet/core/)
- [Entity Framework Core](https://docs.microsoft.com/en-us/ef/core/)
- [SignalR Documentation](https://docs.microsoft.com/en-us/aspnet/core/signalr/)

### Style Guides

- [Microsoft C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- [ASP.NET Core Best Practices](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/best-practices)

## 🎯 Getting Help

If you need help with contributing:

1. **Check the documentation** first
2. **Search existing issues** for similar problems
3. **Create a new issue** with clear details
4. **Join discussions** in GitHub Discussions

## 🙏 Recognition

Contributors will be recognized in:

- **README.md** contributors section
- **Release notes** for significant contributions
- **Project documentation** for major features

---

**Thank you for contributing to Cinema Website FPT777! 🎬**

Your contributions help make this project better for everyone in the community. 