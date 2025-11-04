#!/bin/bash

# Wikipedia MCP Server - Test Runner Script
# This script runs all unit tests, integration tests, and service tests in the project

echo "🧪 Wikipedia MCP Server - Test Runner"
echo "====================================="

# Set script directory as working directory
cd "$(dirname "$0")"

# Check if .NET is available
if ! command -v dotnet &> /dev/null; then
    echo "❌ .NET SDK not found. Please install .NET 8.0 SDK"
    echo "   Download from: https://dotnet.microsoft.com/download"
    exit 1
fi

# Check if we're using .NET 8 SDK (due to global.json)
DOTNET_VERSION=$(dotnet --version)
if [[ ! "$DOTNET_VERSION" =~ ^8\. ]]; then
    echo "⚠️  Warning: Using .NET $DOTNET_VERSION instead of .NET 8"
    echo "   global.json should pin to .NET 8.0.406"
fi

echo "📊 .NET SDK Version: $DOTNET_VERSION"
echo ""

# Function to run a specific test project
run_test_project() {
    local project_name=$1
    local project_path=$2
    local description=$3
    
    echo "🔍 Running $project_name tests..."
    echo "   Description: $description"
    echo "   Path: $project_path"
    
    # Run the tests with detailed output
    dotnet test "$project_path" \
        --verbosity normal \
        --logger "console;verbosity=normal" \
        --configuration Release \
        --collect:"XPlat Code Coverage"
    
    local exit_code=$?
    
    if [ $exit_code -eq 0 ]; then
        echo "✅ $project_name tests PASSED"
    else
        echo "❌ $project_name tests FAILED (exit code: $exit_code)"
        return $exit_code
    fi
    echo ""
}

# Function to run all tests
run_all_tests() {
    echo "🏗️  Building solution first..."
    dotnet build --configuration Release
    
    if [ $? -ne 0 ]; then
        echo "❌ Build failed. Cannot run tests."
        exit 1
    fi
    
    echo "✅ Build successful!"
    echo ""
    
    local total_failures=0
    
    # Run Unit Tests
    run_test_project "Unit" \
        "tests/WikipediaMcpServer.UnitTests/WikipediaMcpServer.UnitTests.csproj" \
        "Core business logic and utility function tests"
    total_failures=$((total_failures + $?))
    
    # Run Service Tests
    run_test_project "Service" \
        "tests/WikipediaMcpServer.ServiceTests/WikipediaMcpServer.ServiceTests.csproj" \
        "Wikipedia service and MCP service layer tests"
    total_failures=$((total_failures + $?))
    
    # Run Integration Tests
    run_test_project "Integration" \
        "tests/WikipediaMcpServer.IntegrationTests/WikipediaMcpServer.IntegrationTests.csproj" \
        "End-to-end API and HTTP endpoint tests"
    total_failures=$((total_failures + $?))
    
    # Run Stdio Tests
    run_test_project "Stdio" \
        "tests/WikipediaMcpServer.StdioTests/WikipediaMcpServer.StdioTests.csproj" \
        "Standard I/O mode and MCP protocol tests"
    total_failures=$((total_failures + $?))
    
    return $total_failures
}

# Function to run tests with coverage
run_tests_with_coverage() {
    echo "📈 Running all tests with code coverage..."
    
    dotnet test \
        --configuration Release \
        --collect:"XPlat Code Coverage" \
        --results-directory:"./TestResults" \
        --logger "trx;LogFileName=TestResults.trx" \
        --verbosity normal
    
    local exit_code=$?
    
    echo ""
    echo "📊 Test results saved to: ./TestResults/"
    echo "🔍 Coverage reports generated in: ./TestResults/*/coverage.cobertura.xml"
    
    return $exit_code
}

# Function to run specific test category
run_specific_tests() {
    local category=$1
    
    case $category in
        "unit")
            run_test_project "Unit" \
                "tests/WikipediaMcpServer.UnitTests/WikipediaMcpServer.UnitTests.csproj" \
                "Core business logic and utility function tests"
            ;;
        "service")
            run_test_project "Service" \
                "tests/WikipediaMcpServer.ServiceTests/WikipediaMcpServer.ServiceTests.csproj" \
                "Wikipedia service and MCP service layer tests"
            ;;
        "integration")
            run_test_project "Integration" \
                "tests/WikipediaMcpServer.IntegrationTests/WikipediaMcpServer.IntegrationTests.csproj" \
                "End-to-end API and HTTP endpoint tests"
            ;;
        "stdio")
            run_test_project "Stdio" \
                "tests/WikipediaMcpServer.StdioTests/WikipediaMcpServer.StdioTests.csproj" \
                "Standard I/O mode and MCP protocol tests"
            ;;
        *)
            echo "❌ Unknown test category: $category"
            echo "   Available categories: unit, service, integration, stdio"
            exit 1
            ;;
    esac
}

# Function to show help
show_help() {
    echo "Usage: $0 [OPTIONS]"
    echo ""
    echo "Options:"
    echo "  --help, -h           Show this help message"
    echo "  --all, -a            Run all test projects (default)"
    echo "  --coverage, -c       Run all tests with code coverage"
    echo "  --unit, -u           Run only unit tests"
    echo "  --service, -s        Run only service tests"
    echo "  --integration, -i    Run only integration tests"
    echo "  --stdio              Run only stdio/MCP protocol tests"
    echo "  --fast, -f           Run tests without building (faster, but may be outdated)"
    echo "  --watch, -w          Run tests in watch mode"
    echo ""
    echo "Examples:"
    echo "  $0                   # Run all tests"
    echo "  $0 --coverage        # Run all tests with coverage"
    echo "  $0 --unit            # Run only unit tests"
    echo "  $0 --integration     # Run only integration tests"
}

# Parse command line arguments
case "${1:-}" in
    "--help"|"-h")
        show_help
        exit 0
        ;;
    "--all"|"-a"|"")
        echo "🎯 Running ALL test projects..."
        echo ""
        run_all_tests
        exit_code=$?
        ;;
    "--coverage"|"-c")
        run_tests_with_coverage
        exit_code=$?
        ;;
    "--unit"|"-u")
        run_specific_tests "unit"
        exit_code=$?
        ;;
    "--service"|"-s")
        run_specific_tests "service"
        exit_code=$?
        ;;
    "--integration"|"-i")
        run_specific_tests "integration"
        exit_code=$?
        ;;
    "--stdio")
        run_specific_tests "stdio"
        exit_code=$?
        ;;
    "--fast"|"-f")
        echo "🏃 Running tests without building (fast mode)..."
        dotnet test --no-build --verbosity normal
        exit_code=$?
        ;;
    "--watch"|"-w")
        echo "👀 Running tests in watch mode..."
        echo "   Press Ctrl+C to stop watching"
        dotnet watch test --verbosity normal
        exit_code=$?
        ;;
    *)
        echo "❌ Unknown option: $1"
        echo ""
        show_help
        exit 1
        ;;
esac

# Final summary
echo ""
echo "==============================================="
if [ $exit_code -eq 0 ]; then
    echo "🎉 ALL TESTS PASSED! ✅"
    echo "   Wikipedia MCP Server is ready for deployment"
else
    echo "❌ SOME TESTS FAILED!"
    echo "   Please check the test output above for details"
    echo "   Exit code: $exit_code"
fi
echo "==============================================="

exit $exit_code