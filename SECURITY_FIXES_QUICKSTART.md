# Security Fixes Quick Start Guide

This document provides a quick reference for implementing the security fixes identified in the security assessment.

## 🔴 CRITICAL Priority Fixes (Do First!)

### 1. Fix Path Traversal Vulnerability

**File:** `src/AssetManager/web/src/main/java/com/microsoft/migration/assets/service/LocalFileStorageService.java`

Add this validation method to the class:

```java
private void validateKey(String key) throws IOException {
    if (key == null || key.isEmpty()) {
        throw new IOException("Invalid file key: key cannot be null or empty");
    }
    
    // Reject any path traversal attempts
    if (key.contains("..") || key.contains("/") || key.contains("\\")) {
        throw new IOException("Invalid file key: path traversal attempt detected");
    }
    
    // Validate the normalized path is within rootLocation
    Path normalizedPath = rootLocation.resolve(key).normalize();
    if (!normalizedPath.startsWith(rootLocation)) {
        throw new IOException("Invalid file key: attempted access outside storage directory");
    }
}
```

Then add `validateKey(key);` at the start of these methods:
- `getObject(String key)` - Line 109
- `deleteObject(String key)` - Line 118

**Test it:**
```bash
# These should all be blocked:
curl "http://localhost:8080/storage/view/../../../etc/passwd"
curl "http://localhost:8080/storage/delete/../../../important/file"
```

---

### 2. Remove Hardcoded Credentials

**File:** `src/AssetManager/web/src/main/resources/application.properties`

Replace hardcoded values with environment variables:

```properties
# AWS S3 Configuration
aws.accessKey=${AWS_ACCESS_KEY:}
aws.secretKey=${AWS_SECRET_KEY:}
aws.region=${AWS_REGION:us-east-1}
aws.s3.bucket=${AWS_S3_BUCKET:}

# RabbitMQ Configuration
spring.rabbitmq.host=${RABBITMQ_HOST:localhost}
spring.rabbitmq.port=${RABBITMQ_PORT:5672}
spring.rabbitmq.username=${RABBITMQ_USERNAME:guest}
spring.rabbitmq.password=${RABBITMQ_PASSWORD:guest}

# Database Configuration
spring.datasource.url=${DATABASE_URL:jdbc:postgresql://localhost:5432/assets_manager}
spring.datasource.username=${DATABASE_USERNAME:postgres}
spring.datasource.password=${DATABASE_PASSWORD:}
```

**Set environment variables before running:**
```bash
export DATABASE_PASSWORD="your_secure_password_here"
export AWS_ACCESS_KEY="your_aws_key"
export AWS_SECRET_KEY="your_aws_secret"
```

**Important:** Rotate all existing credentials as they should be considered compromised!

---

## 🟠 HIGH Priority Fixes (Do Next)

### 3. Add Spring Security

**Step 1:** Add dependency to `src/AssetManager/web/pom.xml`:

```xml
<dependency>
    <groupId>org.springframework.boot</groupId>
    <artifactId>spring-boot-starter-security</artifactId>
</dependency>
```

**Step 2:** Create `src/AssetManager/web/src/main/java/com/microsoft/migration/assets/config/SecurityConfig.java`:

```java
package com.microsoft.migration.assets.config;

import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.security.config.annotation.web.builders.HttpSecurity;
import org.springframework.security.config.annotation.web.configuration.EnableWebSecurity;
import org.springframework.security.web.SecurityFilterChain;

@Configuration
@EnableWebSecurity
public class SecurityConfig {

    @Bean
    public SecurityFilterChain filterChain(HttpSecurity http) throws Exception {
        http
            .csrf().and()  // Enable CSRF protection
            .headers()
                .contentSecurityPolicy("default-src 'self'; script-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net; style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net;")
                .and()
                .xssProtection()
                .and()
                .frameOptions().deny()
                .and()
                .httpStrictTransportSecurity()
                    .includeSubDomains(true)
                    .maxAgeInSeconds(31536000)
            .and()
            .authorizeHttpRequests()
                .anyRequest().permitAll();
        
        return http.build();
    }
}
```

**Step 3:** Add CSRF tokens to HTML forms:

In `src/AssetManager/web/src/main/resources/templates/upload.html` (line 7), add:
```html
<form th:action="@{/storage/upload}" method="post" enctype="multipart/form-data" class="mt-4" id="uploadForm">
    <input type="hidden" th:name="${_csrf.parameterName}" th:value="${_csrf.token}"/>
    <!-- rest of form -->
</form>
```

In `src/AssetManager/web/src/main/resources/templates/list.html` (line 21) and `view.html` (line 24), add:
```html
<form th:action="@{'/storage/delete/' + ${object.key}}" method="post" ...>
    <input type="hidden" th:name="${_csrf.parameterName}" th:value="${_csrf.token}"/>
    <button type="submit" class="btn btn-danger btn-sm">Delete</button>
</form>
```

---

## 🟡 MEDIUM Priority Fixes

### 4. Improve File Upload Validation

**File:** `src/AssetManager/web/src/main/java/com/microsoft/migration/assets/service/LocalFileStorageService.java`

Add these constants to the class:
```java
private static final Set<String> ALLOWED_EXTENSIONS = Set.of(
    "jpg", "jpeg", "png", "gif", "bmp", "webp"
);

private static final Set<String> ALLOWED_MIME_TYPES = Set.of(
    "image/jpeg", "image/png", "image/gif", "image/bmp", "image/webp"
);
```

Add these helper methods:
```java
private String getFileExtension(String filename) {
    int dotIndex = filename.lastIndexOf('.');
    return dotIndex > 0 ? filename.substring(dotIndex + 1) : "";
}

private boolean verifyImageMagicNumbers(MultipartFile file) throws IOException {
    byte[] header = new byte[8];
    try (InputStream is = file.getInputStream()) {
        int read = is.read(header);
        if (read < 2) return false;
        
        // JPEG: FF D8 FF
        if (header[0] == (byte)0xFF && header[1] == (byte)0xD8 && header[2] == (byte)0xFF) {
            return true;
        }
        // PNG: 89 50 4E 47
        if (header[0] == (byte)0x89 && header[1] == (byte)0x50 && 
            header[2] == (byte)0x4E && header[3] == (byte)0x47) {
            return true;
        }
        // GIF: 47 49 46
        if (header[0] == (byte)0x47 && header[1] == (byte)0x49 && header[2] == (byte)0x46) {
            return true;
        }
        // BMP: 42 4D
        if (header[0] == (byte)0x42 && header[1] == (byte)0x4D) {
            return true;
        }
        // WEBP: starts with RIFF
        if (header[0] == (byte)0x52 && header[1] == (byte)0x49 && 
            header[2] == (byte)0x46 && header[3] == (byte)0x46) {
            return true;
        }
    }
    return false;
}
```

Update the `uploadObject` method to include these checks:
```java
@Override
public void uploadObject(MultipartFile file) throws IOException {
    if (file.isEmpty()) {
        throw new IOException("Failed to store empty file");
    }
    
    // Validate file size
    if (file.getSize() > 10 * 1024 * 1024) {
        throw new IOException("File size exceeds maximum allowed size of 10MB");
    }
    
    // Validate MIME type
    String contentType = file.getContentType();
    if (contentType == null || !ALLOWED_MIME_TYPES.contains(contentType.toLowerCase())) {
        throw new IOException("Invalid file type. Only images are allowed.");
    }
    
    // Clean and validate filename
    String filename = StringUtils.cleanPath(file.getOriginalFilename());
    if (filename.contains("..") || filename.contains("/") || filename.contains("\\")) {
        throw new IOException("Invalid filename: path traversal attempt detected");
    }
    
    // Validate file extension
    String extension = getFileExtension(filename).toLowerCase();
    if (!ALLOWED_EXTENSIONS.contains(extension)) {
        throw new IOException("Invalid file extension. Allowed: " + ALLOWED_EXTENSIONS);
    }
    
    // Verify file magic numbers
    if (!verifyImageMagicNumbers(file)) {
        throw new IOException("File content does not match image format");
    }
    
    // Continue with existing code...
    Path targetLocation = rootLocation.resolve(filename);
    Files.copy(file.getInputStream(), targetLocation, StandardCopyOption.REPLACE_EXISTING);
    // ... rest of method
}
```

---

### 5. Add Global Exception Handler

Create `src/AssetManager/web/src/main/java/com/microsoft/migration/assets/exception/GlobalExceptionHandler.java`:

```java
package com.microsoft.migration.assets.exception;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.web.bind.annotation.ControllerAdvice;
import org.springframework.web.bind.annotation.ExceptionHandler;
import org.springframework.web.servlet.ModelAndView;
import org.springframework.web.servlet.mvc.support.RedirectAttributes;

import java.io.FileNotFoundException;

@ControllerAdvice
public class GlobalExceptionHandler {
    
    private static final Logger logger = LoggerFactory.getLogger(GlobalExceptionHandler.class);
    
    @ExceptionHandler(Exception.class)
    public ModelAndView handleException(Exception e, RedirectAttributes redirectAttributes) {
        logger.error("An error occurred", e);
        redirectAttributes.addFlashAttribute("error", 
            "An error occurred while processing your request. Please try again.");
        return new ModelAndView("redirect:/storage");
    }
    
    @ExceptionHandler(FileNotFoundException.class)
    public ModelAndView handleFileNotFound(FileNotFoundException e, RedirectAttributes redirectAttributes) {
        logger.warn("File not found: {}", e.getMessage());
        redirectAttributes.addFlashAttribute("error", "The requested file was not found.");
        return new ModelAndView("redirect:/storage");
    }
}
```

---

### 6. Disable SQL Logging in Production

**File:** `src/AssetManager/web/src/main/resources/application.properties`

Change line 25 from:
```properties
spring.jpa.show-sql=true
```

To:
```properties
spring.jpa.show-sql=${SHOW_SQL:false}
logging.level.org.hibernate.SQL=${SQL_LOG_LEVEL:WARN}
```

---

## Testing Your Fixes

### Test Path Traversal Fix
```bash
# Should return 400/500 errors, not actual files:
curl "http://localhost:8080/storage/view/../../../etc/passwd"
curl "http://localhost:8080/storage/view/%2e%2e%2f%2e%2e%2fetc%2fpasswd"
```

### Test File Upload Validation
```bash
# Should reject non-image files:
echo "malicious content" > test.jsp
curl -F "file=@test.jsp" http://localhost:8080/storage/upload

# Should reject fake images:
cp test.jsp fake.jpg
curl -F "file=@fake.jpg" http://localhost:8080/storage/upload
```

### Test CSRF Protection
1. Open upload form in browser
2. Check page source - should see hidden input with CSRF token
3. Try submitting form without token - should be rejected

### Test Security Headers
```bash
curl -I http://localhost:8080/storage

# Should see headers like:
# X-Content-Type-Options: nosniff
# X-Frame-Options: DENY
# Content-Security-Policy: ...
```

---

## Quick Checklist

- [ ] Add path validation to `LocalFileStorageService.getObject()`
- [ ] Add path validation to `LocalFileStorageService.deleteObject()`
- [ ] Move all credentials to environment variables
- [ ] Rotate all existing credentials
- [ ] Add Spring Security dependency
- [ ] Create `SecurityConfig.java`
- [ ] Add CSRF tokens to all forms
- [ ] Add file type validation to `uploadObject()`
- [ ] Add magic number verification
- [ ] Create `GlobalExceptionHandler.java`
- [ ] Disable SQL logging in production
- [ ] Test all fixes
- [ ] Run security scan

---

---

## 🎯 ACCESSIBILITY Fixes (Critical for Users with Disabilities)

### 8. Add Alternative Text to Images

**File:** `src/AssetManager/web/src/main/resources/templates/list.html`

Update line 10:
```html
<img th:src="@{'/storage/view/' + ${object.key}}" 
     class="card-img-top" 
     th:alt="${object.name}" 
     style="height: 200px; object-fit: cover;">
```

**File:** `src/AssetManager/web/src/main/resources/templates/view.html`

Update line 33:
```html
<img th:src="@{'/storage/view/' + ${object.key}}" 
     class="img-fluid" 
     th:alt="${object.name} + ' - Full size view'" 
     style="max-height: 70vh;">
```

---

### 9. Make Drag-and-Drop Keyboard Accessible

**File:** `src/AssetManager/web/src/main/resources/templates/upload.html`

Update the dropZone div (line 16):
```html
<div id="dropZone" 
     class="border border-dashed border-secondary rounded p-5 text-center"
     tabindex="0"
     role="button"
     aria-label="Click or press Enter to select an image file">
    <i class="bi bi-cloud-upload" style="font-size: 2rem;" aria-hidden="true"></i>
    <p class="mt-2">Drag and drop your image here</p>
    <p class="text-muted">(or press Enter/Space to select a file)</p>
</div>
```

Add keyboard support in the script section:
```javascript
// Add keyboard support for drop zone
dropZone.addEventListener('keydown', function(e) {
    if (e.key === 'Enter' || e.key === ' ') {
        e.preventDefault();
        fileInput.click();
    }
});
```

Add CSS for focus indicator:
```css
#dropZone:focus {
    outline: 3px solid #007bff;
    outline-offset: 2px;
    border-color: #007bff !important;
}
```

---

### 10. Add Skip Navigation Link

**File:** `src/AssetManager/web/src/main/resources/templates/layout.html`

Add after `<body>` tag (after line 20):
```html
<body>
    <!-- Skip navigation link -->
    <a href="#main-content" class="visually-hidden-focusable">
        Skip to main content
    </a>
    
    <div class="container">
```

Add CSS in the `<style>` section:
```css
.visually-hidden-focusable {
    position: absolute;
    left: -10000px;
    width: 1px;
    height: 1px;
    overflow: hidden;
}

.visually-hidden-focusable:focus {
    position: fixed;
    top: 0;
    left: 0;
    width: auto;
    height: auto;
    padding: 10px 20px;
    background: #000;
    color: #fff;
    z-index: 10000;
}
```

---

### 11. Add Language Attribute and Semantic Landmarks

**File:** `src/AssetManager/web/src/main/resources/templates/layout.html`

Update line 2 to add `lang` attribute:
```html
<html xmlns:th="http://www.thymeleaf.org" 
      th:fragment="layout(title, content)"
      lang="en">
```

Wrap the content in `<main>` tag (around line 40):
```html
<!-- Main content landmark -->
<main id="main-content" role="main">
    <div th:replace="${content}">
        Page content goes here
    </div>
</main>
```

Update header section (around line 22) to add nav landmark:
```html
<header class="pb-3 mb-4 border-bottom">
    <div class="d-flex align-items-center text-dark text-decoration-none">
        <h1 class="fs-4">AWS S3 Asset Manager</h1>
        <nav class="ms-auto" aria-label="Primary navigation">
            <a class="btn btn-outline-primary me-2" th:href="@{/storage}">All Images</a>
            <a class="btn btn-primary" th:href="@{/storage/upload}">Upload New Image</a>
        </nav>
    </div>
</header>
```

---

### 12. Add ARIA Labels to Interactive Elements

**File:** `src/AssetManager/web/src/main/resources/templates/list.html`

Update delete button (line 22):
```html
<button type="submit" 
        class="btn btn-danger btn-sm"
        th:aria-label="'Delete ' + ${object.name}">
    Delete
</button>
```

Update view link (line 20):
```html
<a th:href="@{'/storage/view-page/' + ${object.key}}" 
   class="btn btn-primary btn-sm"
   th:aria-label="'View ' + ${object.name}">
    View
</a>
```

---

## Need Help?

Refer to the complete `SECURITY_ASSESSMENT.md` and `ACCESSIBILITY_ASSESSMENT.md` documents for detailed explanations and additional recommendations.

## Additional Resources

- [OWASP Top 10](https://owasp.org/www-project-top-ten/)
- [Spring Security Documentation](https://docs.spring.io/spring-security/reference/index.html)
- [OWASP File Upload Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/File_Upload_Cheat_Sheet.html)
