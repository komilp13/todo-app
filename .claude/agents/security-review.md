# Agent: Security Review

**Subagent Type**: `Explore`

**Purpose**: Audit authentication, authorization, and security vulnerabilities. Identify OWASP top 10 issues and provide remediation.

## When to Use

- Before completing authentication features
- Before production deployment
- After significant security-related changes
- During code review for sensitive features
- Periodic security audits

## Example Prompts

### Example 1: Full Authentication System Review

```
Security audit of our GTD todo app authentication system:

Components to review:
1. PasswordHashingService (PBKDF2 with salt)
2. JwtTokenService (token generation, validation, expiration)
3. User registration endpoint (POST /api/auth/register)
4. User login endpoint (POST /api/auth/login)
5. Current user endpoint (GET /api/auth/me with [Authorize])
6. JWT bearer middleware
7. [Authorize] attribute enforcement
8. CORS configuration
9. JWT secret management
10. Token refresh strategy

Implementation details:
- PBKDF2-SHA256, 100k+ iterations
- 32-byte random salt per user
- JWT signed with HMAC-SHA256
- 24-hour token expiration
- No refresh tokens (yet)
- Secrets stored in appsettings.Development.json

Security checks:
- [ ] Password hashing strength
- [ ] Salt randomness and uniqueness
- [ ] JWT signing key size (min 256 bits?)
- [ ] Token expiration handling
- [ ] Account enumeration prevention
- [ ] Brute force protection
- [ ] SQL injection prevention
- [ ] CORS allowed origins
- [ ] Secure transport (HTTPS enforcement?)
- [ ] Rate limiting on auth endpoints
- [ ] Secure storage of credentials
- [ ] Logout mechanism

Provide:
1. Vulnerability list (if any)
2. Severity ratings (Critical/High/Medium/Low)
3. Specific fix recommendations
4. Code examples of secure implementation
5. Best practices for your stack
6. Compliance notes (OWASP, GDPR, etc.)
```

### Example 2: Authorization Review

```
Review authorization throughout the app:

Features to check:
1. User can only see/edit their own tasks
2. User can only manage their own projects and labels
3. Admin features (if any)
4. Shared/collaborative features (coming later?)

Current implementation:
- [Authorize] on all protected endpoints
- User ID from JWT claims in handlers
- Filtering: WHERE UserId = currentUserId

Potential issues:
1. What if JWT doesn't contain user ID?
2. Can user directly set userId in request?
3. What about deleted users (can they still access)?
4. Is authorization checked in service layer?
5. Any endpoints missing [Authorize]?

Test scenarios:
- User A tries to get User B's task (404? forbidden?)
- User A tries to update User B's project
- User A tries to delete User B's label
- Deleted user tries to access

Provide:
1. Authorization vulnerabilities
2. Secure implementation patterns
3. Test cases for authorization
4. Authorization middleware (if needed)
```

### Example 3: OWASP Top 10 Review

```
Check for OWASP Top 10 vulnerabilities:

1. Broken Access Control
   - Are endpoints properly [Authorize]d?
   - Is user ID filtering implemented everywhere?

2. Cryptographic Failures
   - Password hashing strength?
   - JWT key size?
   - HTTPS enforcement?

3. Injection
   - SQL injection (EF Core parameterization)?
   - API endpoint injection?
   - NoSQL injection (N/A)?

4. Insecure Design
   - Are security requirements documented?
   - Threat modeling done?

5. Security Misconfiguration
   - Swagger exposed in production?
   - Debug mode on?
   - Default credentials?

6. Vulnerable Components
   - Package vulnerabilities?
   - Outdated dependencies?

7. Authentication Failures
   - Rate limiting on login?
   - Account enumeration possible?
   - Weak password requirements?

8. Data Integrity Failures
   - Audit logs for sensitive operations?
   - Data validation comprehensive?

9. Logging & Monitoring Failures
   - Are security events logged?
   - Alerts configured?

10. SSRF (Server-Side Request Forgery)
    - N/A for this app

Provide:
1. Vulnerabilities per category
2. Severity ratings
3. Fix recommendations
4. Code examples
```

## What to Expect from Agent

1. **Vulnerability Assessment**
   ```
   CRITICAL:
   - [ ] JWT secret is only 64 bits (should be 256+)
     Location: appsettings.json
     Fix: Generate 256-bit secret

   HIGH:
   - [ ] No rate limiting on login endpoint
     Risk: Brute force attacks
     Fix: Add RateLimitingMiddleware

   MEDIUM:
   - [ ] Account enumeration via registration
     Issue: Different error messages for existing vs invalid email
     Fix: Use generic error message
   ```

2. **Severity Ratings**
   - **CRITICAL**: Exploitable, data loss risk
   - **HIGH**: Exploitable, limited impact
   - **MEDIUM**: Difficult to exploit, minor impact
   - **LOW**: Very difficult to exploit, cosmetic issues

3. **Code Examples**
   ```csharp
   // ✗ Wrong
   var user = await _context.Users.SingleOrDefaultAsync(u => u.Email == email);
   if (user == null)
       return BadRequest("User not found"); // Account enumeration!

   // ✓ Correct
   var user = await _context.Users.SingleOrDefaultAsync(u => u.Email == email);
   // Always return same message
   if (user == null || !passwordHasher.Verify(password, user.PasswordHash))
       return Unauthorized("Invalid email or password");
   ```

4. **Security Checklist**
   ```
   Authentication:
   - [ ] Passwords hashed with PBKDF2 (min 100k iterations)
   - [ ] Salts unique and random (32+ bytes)
   - [ ] JWT signed with HMAC-SHA256
   - [ ] JWT key >= 256 bits
   - [ ] Token expiration set (24 hours)
   - [ ] No passwords in logs
   - [ ] No credentials in code

   Authorization:
   - [ ] All protected endpoints have [Authorize]
   - [ ] User ID filtering on all queries
   - [ ] No privilege escalation paths
   - [ ] Admin operations restricted
   - [ ] Tests for cross-user access attempts

   Transport Security:
   - [ ] HTTPS enforced in production
   - [ ] HSTS headers set
   - [ ] Secure cookie flags
   - [ ] CORS properly configured

   Input Validation:
   - [ ] All inputs validated
   - [ ] Email format validated
   - [ ] Password complexity enforced
   - [ ] No SQL injection possible (EF Core)
   - [ ] XSS prevention (encoding in frontend)

   Logging & Monitoring:
   - [ ] Login attempts logged
   - [ ] Failed auth logged
   - [ ] Sensitive operations logged
   - [ ] No sensitive data in logs
   ```

5. **Best Practices**
   - PBKDF2 with 100k+ iterations or bcrypt
   - Secure random number generation
   - Never log passwords or tokens
   - Use HTTPS everywhere
   - Implement rate limiting
   - Validate all inputs
   - Use parameterized queries (EF Core does)
   - Implement CORS restrictively

## Remediation Examples

### Fix 1: Strengthen Password Hashing
```csharp
// ✗ Wrong - too few iterations
var hash = PBKDF2(password, salt, 10000);

// ✓ Correct - OWASP recommended
var hash = PBKDF2(password, salt, 600000);

// ✓ Alternative - bcrypt
var hash = BCrypt.HashPassword(password, cost: 12);
```

### Fix 2: Implement Rate Limiting
```csharp
// In middleware
public class RateLimitingMiddleware
{
    public async Task InvokeAsync(HttpContext context)
    {
        var endpoint = context.GetEndpoint();
        if (endpoint?.Metadata.GetMetadata<RateLimitAttribute>() != null)
        {
            // Check rate limit
            if (TooManyRequests(context.Connection.RemoteIpAddress, endpoint.DisplayName))
                context.Response.StatusCode = 429; // Too Many Requests
        }
    }
}
```

### Fix 3: Generic Error Messages
```csharp
// ✗ Wrong - reveals which users exist
if (!userExists) return BadRequest("User not found");
if (!passwordCorrect) return BadRequest("Password incorrect");

// ✓ Correct - same message
if (!userExists || !passwordCorrect)
    return Unauthorized("Invalid email or password");
```

## OWASP Resources

- OWASP Top 10 2021: https://owasp.org/Top10/
- OWASP Authentication Cheat Sheet
- OWASP Authorization Cheat Sheet
- OWASP Secure Coding Practices

## Compliance Considerations

- **GDPR**: User data protection, right to deletion
- **CCPA**: User data rights and transparency
- **HIPAA**: If health data stored (N/A for this app)

## Testing Security

```csharp
[Fact]
public async Task CreateTask_WithoutAuth_Returns401()
{
    var client = CreateUnauthenticatedClient();
    var response = await client.PostAsJsonAsync("/api/tasks", ...);
    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
}

[Fact]
public async Task GetUserBTask_AsOtherUser_Returns404()
{
    var aliceTask = await CreateTaskAsAlice();
    var bobClient = CreateClientForBob();
    var response = await bobClient.GetAsync($"/api/tasks/{aliceTask.Id}");
    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
}

[Fact]
public void PasswordHash_DifferentIterations_ShouldBeSlow()
{
    var sw = Stopwatch.StartNew();
    var hash = HashPassword("password");
    sw.Stop();
    Assert.True(sw.ElapsedMilliseconds > 100, "Hash too fast - increase iterations");
}
```

## Pre-Production Checklist

- [ ] No debug mode in production
- [ ] Swagger disabled in production
- [ ] Secrets in appsettings.Production.json (not in code)
- [ ] HTTPS enforced
- [ ] Security headers set (HSTS, CSP, etc.)
- [ ] Rate limiting enabled
- [ ] Logging configured
- [ ] Error handling doesn't expose internals
- [ ] Dependencies up to date
- [ ] Security testing completed
- [ ] Penetration testing done (if critical)

## Follow-Up Questions

- "What's our password policy?"
- "Do we need MFA (multi-factor auth)?"
- "Should we implement refresh tokens?"
- "What's our secret management strategy?"
- "How long should sessions last?"

## Tips for Using This Agent

1. **Provide code** — Agent can directly audit it
2. **Mention compliance** — GDPR, HIPAA, etc.
3. **Ask about threats** — "What could attackers do?"
4. **Request tests** — "How to test this security feature?"
5. **Get guidance** — "Best practice for JWT refresh?"

## Regular Security Reviews

Schedule periodic reviews:
- After major features
- When dependencies update
- Before production release
- Annually minimum
- After security incidents (if any)

## Resources

- OWASP Top 10
- Microsoft Security Best Practices
- JWT best practices
- PBKDF2/bcrypt documentation
- CORS specification
