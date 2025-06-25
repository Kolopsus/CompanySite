$(document).ready(function() {
    // Security: Initialize CSRF protection
    initializeCSRFProtection();
    
    // Security: Initialize input sanitization
    initializeInputSanitization();
    
    // Set active menu item
    var currentPath = window.location.pathname;
    $('.menu-section a').each(function() {
        var href = $(this).attr('href');
        if (currentPath === href || (currentPath === '/' && href.endsWith('/Home'))) {
            $(this).addClass('active');
        }
    });
    
    // Global AJAX error handler with security considerations
    $(document).ajaxError(function(event, jqXHR, ajaxSettings, thrownError) {
        // Security: Don't expose detailed error information
        var userMessage = getUserFriendlyErrorMessage(jqXHR.status, thrownError);
        showError(userMessage);
        
        // Security: Log error details for debugging (client-side only, no sensitive data)
        console.error('AJAX Error:', {
            status: jqXHR.status,
            statusText: jqXHR.statusText,
            url: ajaxSettings.url,
            timestamp: new Date().toISOString()
        });
    });
});

// Security: Initialize CSRF protection for all AJAX requests
function initializeCSRFProtection() {
    var token = $('meta[name="csrf-token"]').attr('content');
    if (token) {
        // Extract token value from the hidden input
        var tokenMatch = token.match(/value="([^"]+)"/);
        if (tokenMatch && tokenMatch[1]) {
            $.ajaxSetup({
                beforeSend: function(xhr, settings) {
                    // Only add CSRF token for non-GET requests to same origin
                    if (!/^(GET|HEAD|OPTIONS|TRACE)$/i.test(settings.type) && !this.crossDomain) {
                        xhr.setRequestHeader('RequestVerificationToken', tokenMatch[1]);
                    }
                }
            });
        }
    }
}

// Security: Initialize input sanitization for forms
function initializeInputSanitization() {
    // Add input validation and sanitization for all forms
    $('form').on('submit', function(e) {
        var form = $(this);
        var isValid = true;
        
        // Validate all required fields
        form.find('[required]').each(function() {
            var input = $(this);
            if (!input.val().trim()) {
                isValid = false;
                highlightInvalidField(input, 'This field is required.');
            }
        });
        
        // Validate text inputs for basic security
        form.find('input[type="text"], textarea').each(function() {
            var input = $(this);
            var value = input.val();
            
            if (value && !isValidInput(value)) {
                isValid = false;
                highlightInvalidField(input, 'Invalid characters detected.');
            }
        });
        
        if (!isValid) {
            e.preventDefault();
            showError('Please correct the highlighted fields.');
        }
    });
}

// Security: Validate input for dangerous patterns
function isValidInput(input) {
    if (!input || typeof input !== 'string') {
        return false;
    }
    
    // Check for script injection attempts
    var scriptPatterns = [
        /<script\b[^<]*(?:(?!<\/script>)<[^<]*)*<\/script>/gi,
        /javascript:/gi,
        /vbscript:/gi,
        /on\w+\s*=/gi,
        /<iframe/gi,
        /<object/gi,
        /<embed/gi,
        /<link/gi
    ];
    
    for (var i = 0; i < scriptPatterns.length; i++) {
        if (scriptPatterns[i].test(input)) {
            return false;
        }
    }
    
    // Check for SQL injection patterns
    var sqlPatterns = [
        /(\b(select|insert|update|delete|drop|create|alter|exec|execute)\b)|(-{2})|(%27)|(')|(;)|(\|\|)|(\*)/gi
    ];
    
    for (var i = 0; i < sqlPatterns.length; i++) {
        if (sqlPatterns[i].test(input)) {
            return false;
        }
    }
    
    return true;
}

// Security: Sanitize HTML output
function sanitizeHtml(input) {
    if (!input) return '';
    
    var temp = document.createElement('div');
    temp.textContent = input;
    return temp.innerHTML;
}

// Security: Safe HTML insertion
function safeHtmlInsert(element, content) {
    if (typeof content === 'string') {
        element.text(content); // Use text() instead of html() for user content
    } else {
        element.empty().append(content);
    }
}

// Security: Get user-friendly error messages without exposing system details
function getUserFriendlyErrorMessage(status, thrownError) {
    switch (status) {
        case 0:
            return 'Unable to connect to server. Please check your internet connection.';
        case 400:
            return 'Invalid request. Please check your input and try again.';
        case 401:
            return 'You are not authorized to perform this action. Please log in.';
        case 403:
            return 'Access denied. You do not have permission to perform this action.';
        case 404:
            return 'The requested resource was not found.';
        case 429:
            return 'Too many requests. Please wait a moment and try again.';
        case 500:
        case 502:
        case 503:
        case 504:
            return 'Server error. Please try again later or contact support.';
        default:
            if (thrownError === 'parsererror') {
                return 'Error processing server response. Please try again.';
            } else if (thrownError === 'timeout') {
                return 'Request timed out. Please try again.';
            } else if (thrownError === 'abort') {
                return 'Request was cancelled.';
            } else {
                return 'An unexpected error occurred. Please try again.';
            }
    }
}

// Security: Enhanced error display function
function showError(message) {
    // Sanitize the message before displaying
    var sanitizedMessage = sanitizeHtml(message);
    
    // Check if we have a response that's JSON with additional security checks
    try {
        if (typeof message === 'string' && message.trim().startsWith('{')) {
            var response = JSON.parse(message);
            if (response && response.message && typeof response.message === 'string') {
                sanitizedMessage = sanitizeHtml(response.message);
            }
        }
    } catch (e) {
        // Not JSON, continue with sanitized message
    }
    
    // Use a secure alert or custom modal instead of basic alert
    if (typeof showSecureModal === 'function') {
        showSecureModal('Error', sanitizedMessage);
    } else {
        alert(sanitizedMessage);
    }
}

// Security: Highlight invalid form fields
function highlightInvalidField(field, message) {
    field.addClass('is-invalid');
    
    // Remove existing error message
    field.siblings('.invalid-feedback').remove();
    
    // Add new error message
    var errorDiv = $('<div class="invalid-feedback"></div>');
    errorDiv.text(message); // Use text() for security
    field.after(errorDiv);
    
    // Remove highlight after user starts typing
    field.one('input', function() {
        $(this).removeClass('is-invalid');
        $(this).siblings('.invalid-feedback').remove();
    });
}

// Security: Rate limiting for AJAX requests
var ajaxRequestCount = 0;
var ajaxRequestWindow = Date.now();
var maxRequestsPerMinute = 30;

function checkRateLimit() {
    var now = Date.now();
    
    // Reset counter every minute
    if (now - ajaxRequestWindow > 60000) {
        ajaxRequestCount = 0;
        ajaxRequestWindow = now;
    }
    
    ajaxRequestCount++;
    
    if (ajaxRequestCount > maxRequestsPerMinute) {
        showError('Too many requests. Please wait a moment before trying again.');
        return false;
    }
    
    return true;
}

// Security: Override jQuery AJAX to add rate limiting
var originalAjax = $.ajax;
$.ajax = function(options) {
    if (!checkRateLimit()) {
        return $.Deferred().reject('Rate limit exceeded').promise();
    }
    
    // Add timeout if not specified
    if (!options.timeout) {
        options.timeout = 30000; // 30 seconds
    }
    
    return originalAjax.call(this, options);
};

// Security: Enhanced window error handler
window.onerror = function(msg, url, lineNo, columnNo, error) {
    // Log error for debugging but don't expose sensitive information
    console.error('JavaScript Error:', {
        message: msg,
        line: lineNo,
        column: columnNo,
        timestamp: new Date().toISOString()
    });
    
    // Don't expose error details to user
    showError('An unexpected error occurred. Please refresh the page and try again.');
    
    return true; // Prevent default browser error handling
};

// Security: Prevent console access in production
if (window.location.hostname !== 'localhost' && window.location.hostname !== '127.0.0.1') {
    var devtools = {
        open: false,
        orientation: null
    };
    
    setInterval(function() {
        if (window.outerHeight - window.innerHeight > 200 || window.outerWidth - window.innerWidth > 200) {
            if (!devtools.open) {
                devtools.open = true;
                console.clear();
                console.log('%cDeveloper tools detected! For security reasons, please close the developer console.', 'color: red; font-size: 20px; font-weight: bold;');
            }
        } else {
            devtools.open = false;
        }
    }, 500);
}

// Security: Prevent drag and drop of files to prevent potential attacks
$(document).on('dragover drop', function(e) {
    e.preventDefault();
    return false;
});

// Security: Clear sensitive form data on page unload
$(window).on('beforeunload', function() {
    $('input[type="password"], input[name*="password"], input[name*="secret"]').val('');
});