#!/usr/bin/env python3
"""
Wikipedia MCP Server - Test Runner Script
This script runs all unit tests, integration tests, and service tests in the project
"""

import os
import sys
import subprocess
import argparse
import platform

def print_banner():
    print("🧪 Wikipedia MCP Server - Test Runner")
    print("=" * 37)

def check_dotnet():
    """Check if .NET SDK is available and show version"""
    try:
        result = subprocess.run(['dotnet', '--version'], 
                              capture_output=True, text=True, check=True)
        version = result.stdout.strip()
        print(f"📊 .NET SDK Version: {version}")
        
        # Check if using .NET 8 (due to global.json)
        if not version.startswith('8.'):
            print(f"⚠️  Warning: Using .NET {version} instead of .NET 8")
            print("   global.json should pin to .NET 8.0.406")
        
        print()
        return True
    except (subprocess.CalledProcessError, FileNotFoundError):
        print("❌ .NET SDK not found. Please install .NET 8.0 SDK")
        print("   Download from: https://dotnet.microsoft.com/download")
        return False

def run_test_project(project_name, project_path, description):
    """Run a specific test project"""
    print(f"🔍 Running {project_name} tests...")
    print(f"   Description: {description}")
    print(f"   Path: {project_path}")
    
    try:
        result = subprocess.run([
            'dotnet', 'test', project_path,
            '--verbosity', 'normal',
            '--logger', 'console;verbosity=normal',
            '--configuration', 'Release',
            '--no-build',
            '--collect:XPlat Code Coverage'
        ], check=True, capture_output=False)
        
        print(f"✅ {project_name} tests PASSED")
        return 0
    except subprocess.CalledProcessError as e:
        print(f"❌ {project_name} tests FAILED (exit code: {e.returncode})")
        return e.returncode
    finally:
        print()

def build_solution():
    """Build the solution"""
    print("🏗️  Building solution first...")
    try:
        subprocess.run(['dotnet', 'build', '--configuration', 'Release'], check=True)
        print("✅ Build successful!")
        print()
        return True
    except subprocess.CalledProcessError:
        print("❌ Build failed. Cannot run tests.")
        return False

def run_all_tests():
    """Run all test projects"""
    print("🎯 Running ALL test projects...")
    print()
    
    if not build_solution():
        return 1
    
    test_projects = [
        ("Unit", "tests/WikipediaMcpServer.UnitTests/WikipediaMcpServer.UnitTests.csproj", 
         "Core business logic and utility function tests"),
        ("Service", "tests/WikipediaMcpServer.ServiceTests/WikipediaMcpServer.ServiceTests.csproj", 
         "Wikipedia service and MCP service layer tests"),
        ("Integration", "tests/WikipediaMcpServer.IntegrationTests/WikipediaMcpServer.IntegrationTests.csproj", 
         "End-to-end API and HTTP endpoint tests"),
        ("Stdio", "tests/WikipediaMcpServer.StdioTests/WikipediaMcpServer.StdioTests.csproj", 
         "Standard I/O mode and MCP protocol tests")
    ]
    
    total_failures = 0
    for name, path, desc in test_projects:
        total_failures += run_test_project(name, path, desc)
    
    return total_failures

def run_tests_with_coverage():
    """Run all tests with code coverage"""
    print("📈 Running all tests with code coverage...")
    
    try:
        subprocess.run([
            'dotnet', 'test',
            '--configuration', 'Release',
            '--collect:XPlat Code Coverage',
            '--results-directory', './TestResults',
            '--logger', 'trx;LogFileName=TestResults.trx',
            '--verbosity', 'normal'
        ], check=True)
        
        print()
        print("📊 Test results saved to: ./TestResults/")
        print("🔍 Coverage reports generated in: ./TestResults/*/coverage.cobertura.xml")
        return 0
    except subprocess.CalledProcessError as e:
        return e.returncode

def run_specific_tests(category):
    """Run specific test category"""
    test_configs = {
        'unit': ("Unit", "tests/WikipediaMcpServer.UnitTests/WikipediaMcpServer.UnitTests.csproj", 
                "Core business logic and utility function tests"),
        'service': ("Service", "tests/WikipediaMcpServer.ServiceTests/WikipediaMcpServer.ServiceTests.csproj", 
                   "Wikipedia service and MCP service layer tests"),
        'integration': ("Integration", "tests/WikipediaMcpServer.IntegrationTests/WikipediaMcpServer.IntegrationTests.csproj", 
                       "End-to-end API and HTTP endpoint tests"),
        'stdio': ("Stdio", "tests/WikipediaMcpServer.StdioTests/WikipediaMcpServer.StdioTests.csproj", 
                 "Standard I/O mode and MCP protocol tests")
    }
    
    if category not in test_configs:
        print(f"❌ Unknown test category: {category}")
        print("   Available categories: unit, service, integration, stdio")
        return 1
    
    name, path, desc = test_configs[category]
    return run_test_project(name, path, desc)

def run_fast_tests():
    """Run tests without building"""
    print("🏃 Running tests without building (fast mode)...")
    try:
        subprocess.run(['dotnet', 'test', '--no-build', '--verbosity', 'normal'], check=True)
        return 0
    except subprocess.CalledProcessError as e:
        return e.returncode

def run_watch_tests():
    """Run tests in watch mode"""
    print("👀 Running tests in watch mode...")
    print("   Press Ctrl+C to stop watching")
    try:
        subprocess.run(['dotnet', 'watch', 'test', '--verbosity', 'normal'], check=True)
        return 0
    except subprocess.CalledProcessError as e:
        return e.returncode
    except KeyboardInterrupt:
        print("\n🛑 Watch mode stopped by user")
        return 0

def print_summary(exit_code):
    """Print final test summary"""
    print()
    print("=" * 47)
    if exit_code == 0:
        print("🎉 ALL TESTS PASSED! ✅")
        print("   Wikipedia MCP Server is ready for deployment")
    else:
        print("❌ SOME TESTS FAILED!")
        print("   Please check the test output above for details")
        print(f"   Exit code: {exit_code}")
    print("=" * 47)

def main():
    # Change to script directory
    script_dir = os.path.dirname(os.path.abspath(__file__))
    os.chdir(script_dir)
    
    # Parse arguments
    parser = argparse.ArgumentParser(description='Run Wikipedia MCP Server tests')
    parser.add_argument('--all', '-a', action='store_true', help='Run all test projects (default)')
    parser.add_argument('--coverage', '-c', action='store_true', help='Run all tests with code coverage')
    parser.add_argument('--unit', '-u', action='store_true', help='Run only unit tests')
    parser.add_argument('--service', '-s', action='store_true', help='Run only service tests')
    parser.add_argument('--integration', '-i', action='store_true', help='Run only integration tests')
    parser.add_argument('--stdio', action='store_true', help='Run only stdio/MCP protocol tests')
    parser.add_argument('--fast', '-f', action='store_true', help='Run tests without building')
    parser.add_argument('--watch', '-w', action='store_true', help='Run tests in watch mode')
    
    args = parser.parse_args()
    
    print_banner()
    
    # Check prerequisites
    if not check_dotnet():
        sys.exit(1)
    
    # Determine what to run
    exit_code = 0
    
    if args.coverage:
        exit_code = run_tests_with_coverage()
    elif args.unit:
        exit_code = run_specific_tests('unit')
    elif args.service:
        exit_code = run_specific_tests('service')
    elif args.integration:
        exit_code = run_specific_tests('integration')
    elif args.stdio:
        exit_code = run_specific_tests('stdio')
    elif args.fast:
        exit_code = run_fast_tests()
    elif args.watch:
        exit_code = run_watch_tests()
    else:
        # Default: run all tests
        exit_code = run_all_tests()
    
    print_summary(exit_code)
    sys.exit(exit_code)

if __name__ == "__main__":
    main()