# 📋 TODO: Test Coverage Improvements

**Created:** November 2, 2025  
**Priority:** High  
**Estimated Effort:** 2-3 days  

## 🎯 Overview

While our current **264 tests provide excellent MCP compliance coverage (100% passing)**, this document outlines critical test gaps identified during security and robustness analysis. These tests focus on **real-world adversarial conditions** and **production edge cases** that could cause failures.

---

## 🔴 **CRITICAL PRIORITY - Security & Input Validation Tests**

### Missing Security Tests
- [ ] **SQL Injection patterns** in search queries
- [ ] **XSS prevention** in Wikipedia content parsing  
- [ ] **Large payload attacks** (JSON bombs, massive search queries)
- [ ] **Unicode/encoding edge cases** (special characters, emojis, various languages)
- [ ] **Path traversal attempts** in topic/section parameters
- [ ] **Rate limiting** and DOS protection tests

```csharp
// TODO: Implement these critical security tests
[Theory]
[InlineData("'; DROP TABLE users; --")]
[InlineData("<script>alert('xss')</script>")]
[InlineData("../../../etc/passwd")]
[InlineData("🚀💀🔥" + new string('A', 100000))]
public async Task SearchAsync_WithMaliciousInput_ShouldBeHandledSafely(string maliciousQuery)

[Fact]
public async Task WikipediaService_MassivePayload_ShouldNotCauseOutOfMemory()
{
    // Test with 100MB+ query strings
    // Verify graceful rejection without memory exhaustion
}
```

---

## 🟠 **HIGH PRIORITY - Performance & Resource Management Tests**

### Missing Performance Tests
- [ ] **Memory leak tests** for long-running operations
- [ ] **Concurrent request handling** (multiple simultaneous Wikipedia API calls)
- [ ] **Large response handling** (huge Wikipedia articles)
- [ ] **Connection pooling exhaustion** scenarios
- [ ] **Timeout and cancellation under load**
- [ ] **Resource disposal verification** in failure scenarios

```csharp
// TODO: Critical performance tests
[Fact]
public async Task WikipediaService_ConcurrentRequests_ShouldNotCauseDeadlock()
{
    // Launch 1000 concurrent requests
    // Monitor for deadlocks and thread starvation
}

[Fact] 
public async Task WikipediaService_LargeArticle_ShouldHandleMemoryEfficiently()
{
    // Test with 50MB+ Wikipedia articles
    // Verify memory usage stays reasonable
}

[Fact]
public async Task WikipediaService_UnderHighConcurrency_ShouldNotLeakMemory()
{
    // 1000+ concurrent operations
    // Monitor memory usage patterns
    // Assert no resource leaks
}
```

---

## 🟡 **MEDIUM PRIORITY - Real-World Network Failure Scenarios**

### Missing Network Resilience Tests
- [ ] **Intermittent network failures** (partial responses, connection drops)
- [ ] **Wikipedia API rate limiting responses** (429 status codes)
- [ ] **DNS resolution failures**
- [ ] **Certificate validation errors**
- [ ] **Proxy/firewall scenarios**
- [ ] **Wikipedia maintenance mode responses**

```csharp
// TODO: Network resilience tests
[Fact]
public async Task WikipediaService_WikipediaRateLimit_ShouldHandleGracefully()
{
    // Mock 429 rate limit responses
    // Verify exponential backoff
}

[Fact]
public async Task WikipediaService_IntermittentFailures_ShouldRetryCorrectly()
{
    // Simulate connection drops mid-response
    // Test retry logic
}
```

---

## 🟡 **MEDIUM PRIORITY - Configuration & Environment Tests**

### Missing Environment Tests
- [ ] **Environment variable injection** tests
- [ ] **Configuration validation** (missing/invalid settings)
- [ ] **Different .NET runtime versions** compatibility
- [ ] **Culture/localization impacts** on parsing
- [ ] **Time zone sensitive operations**

```csharp
// TODO: Environment robustness tests
[Theory]
[InlineData("en-US")]
[InlineData("ar-SA")] // Right-to-left
[InlineData("zh-CN")] // Chinese
[InlineData("ru-RU")] // Cyrillic
public async Task WikipediaService_DifferentLocales_ShouldParseCorrectly(string culture)
```

---

## 🟢 **LOW PRIORITY - MCP Protocol Stress Tests**

### Missing Protocol Edge Cases
- [ ] **Protocol version negotiation edge cases** (partial responses, corrupted handshakes)
- [ ] **Large tool argument payloads**
- [ ] **Rapid successive tool calls**
- [ ] **Protocol state corruption scenarios**
- [ ] **Microsoft SDK vs custom implementation consistency tests**

```csharp
// TODO: MCP protocol stress tests
[Fact]
public async Task McpProtocol_RapidToolCalls_ShouldHandleCorrectly()
{
    // 100 tool calls in rapid succession
    // Verify no state corruption
}

[Fact]
public async Task McpProtocol_LargeToolArguments_ShouldNotTimeout()
{
    // 10MB+ tool arguments
    // Test serialization limits
}
```

---

## 🟢 **LOW PRIORITY - Wikipedia API Specific Edge Cases**

### Missing Wikipedia API Tests  
- [ ] **Wikipedia disambiguation pages** handling
- [ ] **Redirect loops** in Wikipedia links
- [ ] **Articles with complex formatting** (tables, math formulas, special templates)
- [ ] **Non-English Wikipedia API responses** (when English fails)
- [ ] **Wikipedia API versioning changes** impact

```csharp
// TODO: Wikipedia-specific edge cases
[Fact]
public async Task WikipediaService_DisambiguationPage_ShouldProvideOptions()
{
    // Test with "Mercury" (planet vs element vs god)
    // Verify disambiguation handling
}

[Fact]
public async Task WikipediaService_ComplexFormatting_ShouldParseCleanly()
{
    // Test with math equations, tables, templates
    // Verify clean text extraction
}
```

---

## 🟢 **LOW PRIORITY - Integration & Cross-Platform Tests**

### Missing Integration Tests
- [ ] **End-to-end user scenarios** (typical AI assistant workflows)
- [ ] **VS Code extension integration** tests
- [ ] **Claude Desktop integration** validation
- [ ] **Multiple MCP clients** connecting simultaneously
- [ ] **Session state management** across multiple tool calls

### Missing Cross-Platform Tests
- [ ] **macOS vs Windows vs Linux** behavior differences
- [ ] **Different .NET implementations** (Framework vs Core vs 5+)
- [ ] **Container environment** testing (Docker, Kubernetes)
- [ ] **ARM vs x64 architecture** differences

---

## 📊 **Implementation Plan**

### Phase 1: Security & Performance (Day 1)
1. **Security input validation tests** (highest priority)
2. **Concurrent request handling tests**
3. **Memory leak detection tests**

### Phase 2: Network Resilience (Day 2)
1. **Wikipedia API failure scenarios**
2. **Network timeout/retry logic**
3. **Rate limiting handling**

### Phase 3: Edge Cases & Polish (Day 3)
1. **Protocol stress tests**
2. **Wikipedia-specific edge cases**
3. **Cross-platform compatibility**

---

## 📁 **File Locations for New Tests**

```
tests/
├── WikipediaMcpServer.SecurityTests/           # NEW - Security tests
│   ├── InputValidationTests.cs
│   ├── PayloadAttackTests.cs
│   └── XssPreventionTests.cs
├── WikipediaMcpServer.PerformanceTests/        # NEW - Performance tests
│   ├── ConcurrencyTests.cs
│   ├── MemoryLeakTests.cs
│   └── LoadTests.cs
├── WikipediaMcpServer.ResilienceTests/         # NEW - Network resilience
│   ├── NetworkFailureTests.cs
│   ├── WikipediaApiEdgeCaseTests.cs
│   └── RetryLogicTests.cs
└── WikipediaMcpServer.IntegrationTests/        # EXISTING - Add E2E tests
    └── EndToEndScenarioTests.cs               # NEW
```

---

## 🎯 **Success Criteria**

After implementation:
- [ ] **350+ total tests** (up from current 264)
- [ ] **Security vulnerabilities identified** and protected against
- [ ] **Performance bottlenecks discovered** and documented
- [ ] **Production failure scenarios** covered and handled gracefully
- [ ] **Comprehensive test report** with coverage analysis

---

## 🚨 **Current Status**

- ✅ **MCP Compliance**: 100% (264/264 tests passing)
- ❌ **Security Testing**: 0% (no dedicated security tests)
- ❌ **Performance Testing**: 5% (basic timeout tests only)
- ❌ **Network Resilience**: 10% (basic HTTP error tests only)
- ❌ **Production Readiness**: 70% (missing critical edge cases)

**Overall Test Maturity: 60%** - Good for MVP, needs hardening for production.

---

## 📝 **Notes**

- All current tests remain passing ✅
- These additions are **supplementary** to existing coverage
- Focus on **real-world production scenarios** vs happy path testing
- **Security tests are highest priority** - could prevent production incidents
- Some tests may require **external tooling** (memory profilers, load generators)

---

**Next Action:** Pick a time tomorrow to tackle Phase 1 (Security & Performance tests)