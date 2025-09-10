using System.Reflection;
using coptic_app_backend.Application.Services;
using coptic_app_backend.Domain.Interfaces;
using coptic_app_backend.Infrastructure.Services;
using coptic_app_backend.Infrastructure.Repositories;
using coptic_app_backend.Infrastructure.Data;
using coptic_app_backend.Api.Hubs;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using System.Text.RegularExpressions;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// JSON Repair Utility for malformed Firebase credentials
static string RepairMalformedJson(string malformedJson)
{
    try
    {
        // First, try to parse as-is
        JsonDocument.Parse(malformedJson);
        return malformedJson; // Already valid JSON
    }
    catch
    {
        Console.WriteLine("[JSON Repair] Detected malformed JSON, attempting repair...");
        
        // Remove outer braces if present
        var content = malformedJson.Trim().TrimStart('{').TrimEnd('}');
        
        // Split by commas, but be careful with private key content
        var parts = new List<string>();
        var currentPart = new StringBuilder();
        var inPrivateKey = false;
        var braceCount = 0;
        
        for (int i = 0; i < content.Length; i++)
        {
            var currentChar = content[i];
            
            if (currentChar == '{') braceCount++;
            if (currentChar == '}') braceCount--;
            
            if (currentChar == ',' && !inPrivateKey && braceCount == 0)
            {
                parts.Add(currentPart.ToString().Trim());
                currentPart.Clear();
            }
            else
            {
                currentPart.Append(currentChar);
                
                // Check if we're entering private key section
                if (currentPart.ToString().Contains("private_key") && 
                    currentPart.ToString().Contains("-----BEGIN PRIVATE KEY-----"))
                {
                    inPrivateKey = true;
                }
                // Check if we're exiting private key section
                else if (inPrivateKey && currentPart.ToString().Contains("-----END PRIVATE KEY-----"))
                {
                    inPrivateKey = false;
                }
            }
        }
        
        // Add the last part
        if (currentPart.Length > 0)
        {
            parts.Add(currentPart.ToString().Trim());
        }
        
        // Reconstruct JSON
        var jsonBuilder = new StringBuilder();
        jsonBuilder.Append("{");
        
        for (int i = 0; i < parts.Count; i++)
        {
            var part = parts[i].Trim();
            if (string.IsNullOrEmpty(part)) continue;
            
            if (i > 0) jsonBuilder.Append(",");
            
            // Find the first colon to split key and value
            var colonIndex = part.IndexOf(':');
            if (colonIndex > 0)
            {
                var key = part.Substring(0, colonIndex).Trim();
                var value = part.Substring(colonIndex + 1).Trim().TrimEnd(',');
                
                // Ensure key is quoted
                if (!key.StartsWith("\"")) key = $"\"{key}\"";
                
                // Handle special cases for values
                if (key == "\"private_key\"")
                {
                    // Fix newlines in private key and remove carriage returns
                    value = value.Replace("\\n", "\n");
                    value = value.Replace("\r", ""); // Remove carriage returns
                    
                    // More precise approach: only replace specific patterns that should be newlines
                    // Based on the valid JSON structure, we know these specific patterns should be newlines
                    value = value.Replace("nMII", "\nMII");           // First base64 line
                    value = value.Replace("n1II", "\n1II");           // Alternative first base64 line pattern
                    value = value.Replace("n4Bq4", "\n4Bq4");         // Second base64 line  
                    value = value.Replace("nB+PR", "\nB+PR");         // Another line
                    value = value.Replace("n1Lie", "\n1Lie");         // Another line
                    value = value.Replace("nA7JO", "\nA7JO");         // Another line
                    value = value.Replace("n758l", "\n758l");         // Another line
                    value = value.Replace("n-----END", "\n-----END"); // End marker
                    
                    value = value.Replace("\n", "\\n"); // Escape newlines for JSON
                    value = $"\"{value}\"";
                }
                else if (key == "\"project_id\"" || key == "\"private_key_id\"" || 
                         key == "\"client_email\"" || key == "\"client_id\"" || 
                         key == "\"auth_uri\"" || key == "\"token_uri\"" || 
                         key == "\"auth_provider_x509_cert_url\"" || key == "\"client_x509_cert_url\"")
                {
                    // Ensure string values are quoted
                    if (!value.StartsWith("\"")) value = $"\"{value}\"";
                }
                else if (key == "\"type\"")
                {
                    // Ensure string values are quoted
                    if (!value.StartsWith("\"")) value = $"\"{value}\"";
                }
                else
                {
                    // For other values, try to determine if they should be quoted
                    if (value.StartsWith("\"") && value.EndsWith("\""))
                    {
                        // Already quoted, keep as is
                    }
                    else if (value == "true" || value == "false" || 
                             (int.TryParse(value, out _)) || 
                             (double.TryParse(value, out _)))
                    {
                        // Keep as unquoted (boolean/number)
                    }
                    else
                    {
                        // Quote as string
                        value = $"\"{value}\"";
                    }
                }
                
                jsonBuilder.Append($"{key}:{value}");
            }
        }
        
        jsonBuilder.Append("}");
        
        var repairedJson = jsonBuilder.ToString();
        
        // Validate the repaired JSON
        try
        {
            JsonDocument.Parse(repairedJson);
            Console.WriteLine("[JSON Repair] Successfully repaired malformed JSON");
            return repairedJson;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[JSON Repair] Failed to repair JSON: {ex.Message}");
            throw;
        }
    }
}

// Only initialize Firebase in the Production environment

if (builder.Environment.IsProduction())
{
    var credentialPath = Path.Combine(AppContext.BaseDirectory, "firebase", "service-account.json");

    if (!File.Exists(credentialPath))
    {
        Console.WriteLine($"[Firebase Init] File not found: {credentialPath}");
    }
    else if (FirebaseApp.DefaultInstance == null)
    {
        try
        {
            var jsonText = File.ReadAllText(credentialPath).Trim();

            // Log only first 200 characters to avoid leaking private key
            var preview = jsonText.Length > 200 ? jsonText.Substring(0, 200) + "..." : jsonText;
            Console.WriteLine($"[Firebase Init] Credential file preview: {preview}");

            // Try to repair malformed JSON if needed
            string jsonToUse;
            try
            {
                // First try to use the JSON as-is (in case CI/CD already fixed it)
                GoogleCredential.FromJson(jsonText);
                jsonToUse = jsonText;
                Console.WriteLine("[Firebase Init] Using JSON as-is (already valid)");
            }
            catch
            {
                // If that fails, try to repair it
                Console.WriteLine("[Firebase Init] JSON parsing failed, attempting repair...");
                jsonToUse = RepairMalformedJson(jsonText);
            }

            var credential = GoogleCredential.FromJson(jsonToUse);
            FirebaseApp.Create(new AppOptions { Credential = credential });
            Console.WriteLine("[Firebase Init] Firebase initialized successfully!");
        }
        catch (Newtonsoft.Json.JsonReaderException jex)
        {
            Console.Error.WriteLine($"[Firebase Init] JSON deserialization failed: {jex.Message}");
        }
        catch (InvalidOperationException ioex) when (ioex.Message.Contains("Error deserializing JSON credential data"))
        {
            Console.WriteLine($"[Firebase Init] Invalid JSON credential data. Skipping Firebase initialization. {ioex.Message}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[Firebase Init] Unexpected error during Firebase initialization: {ex.Message}");
        }
    }
    else
    {
        Console.WriteLine("[Firebase Init] Firebase already initialized.");
    }
}

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Coptic App Backend API",
        Version = "v1",
        Description = "API for Coptic App Backend with JWT Authentication"
    });

    // Include XML comments for better documentation
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }

    // Add JWT Authentication to Swagger
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });

    // Add operation filters for better documentation
    // c.OperationFilter<coptic_app_backend.Api.Filters.SwaggerDefaultValues>();
});

// Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwtSettings["Key"] ?? "your-super-secret-key-with-at-least-32-characters");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    
    // Add debug logging
    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            Console.WriteLine($"JWT Authentication Failed: {context.Exception.Message}");
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            Console.WriteLine($"JWT Token Validated Successfully for user: {context.Principal?.Identity?.Name}");
            return Task.CompletedTask;
        },
        OnChallenge = context =>
        {
            Console.WriteLine($"JWT Challenge: {context.Error}, {context.ErrorDescription}");
            return Task.CompletedTask;
        }
    };
    
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings["Issuer"] ?? "coptic-app-backend",
        ValidateAudience = true,
        ValidAudience = jwtSettings["Audience"] ?? "coptic-app-frontend",
        ClockSkew = TimeSpan.Zero
    };
});

// Configure Authorization Policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AbuneOnly", policy => policy.RequireClaim("UserType", "Abune"));
    options.AddPolicy("RegularUserOnly", policy => policy.RequireClaim("UserType", "Regular"));
    options.AddPolicy("AuthenticatedUser", policy => policy.RequireAuthenticatedUser());
});

// Add SignalR for WebSocket support
builder.Services.AddSignalR();

// Configure PostgreSQL
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"), 
        b => b.MigrationsAssembly("coptic_app_backend.Api")));

builder.Services.AddHttpClient();

            // Configure infrastructure services (PostgreSQL implementations)
            builder.Services.AddScoped<IChatRepository, PostgreSQLChatRepository>();
            builder.Services.AddScoped<IUserRepository, PostgreSQLUserRepository>();
            
            // Configure AwsCognitoService with JWT settings
            builder.Services.AddScoped<ICognitoUserService>(provider => 
            {
                var configuration = provider.GetRequiredService<IConfiguration>();
                var logger = provider.GetRequiredService<ILogger<AwsCognitoService>>();
                return new AwsCognitoService(
                    provider.GetRequiredService<IUserRepository>(),
                    configuration,
                    logger
                );
            });
            
            builder.Services.AddScoped<IAbuneService, AbuneService>();
            builder.Services.AddScoped<INotificationService, FCMNotificationService>();
            builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();

// Configure application services
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddScoped<IUserService, UserService>();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.WithOrigins(
                "http://localhost:3000", 
                "http://localhost:5173", 
                "http://localhost:5199",
                "https://localhost:7061",
                "https://localhost:44364",
                "http://162.243.165.212:5000",
                "https://d89233c7fbfaede4d16e1cd39e342d3f.serveo.net", // Allow all serveo.net subdomains
                "https://*.loca.lt"     // Allow all loca.lt subdomains
              )
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials(); // Required for SignalR
    });
    
    // Add a more permissive policy for development
    options.AddPolicy("AllowAllDev", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
// Enable Swagger in both Development and Production for API documentation
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Coptic App Backend API v1");
    c.RoutePrefix = "swagger";
});

// Only use HTTPS redirection in production
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
    app.UseCors("AllowAll");
}
else
{
    // Use more permissive CORS in development
    app.UseCors("AllowAllDev");
}

// Serve static files (for the HTML test client and uploads)
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

// Map controllers and SignalR hub
app.MapControllers();
app.MapHub<ChatHub>("/chatHub");

// Add health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}