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
        
        // Simple approach: split by lines and process each field
        var lines = malformedJson.Replace("{", "").Replace("}", "").Split(',');
        var data = new Dictionary<string, string>();
        
        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();
            if (trimmedLine.Contains(":"))
            {
                var colonIndex = trimmedLine.IndexOf(':');
                var key = trimmedLine.Substring(0, colonIndex).Trim().Trim('"', '\'');
                var value = trimmedLine.Substring(colonIndex + 1).Trim().Trim('"', '\'');
                
                // Handle private key specially
                if (key == "private_key")
                {
                    Console.WriteLine($"[JSON Repair] Original private key length: {value.Length}");
                    Console.WriteLine($"[JSON Repair] Private key preview: {value.Substring(0, Math.Min(100, value.Length))}...");
                    
                    // Clean up the private key first
                    value = value.Trim();
                    
                    // Remove any existing BEGIN/END markers to avoid duplication
                    value = value.Replace("-----BEGIN PRIVATE KEY-----", "").Replace("-----END PRIVATE KEY-----", "").Trim();
                    
                    // Fix newlines in private key - be more aggressive
                    value = value.Replace("nMII", "\nMII");
                    value = value.Replace("n1II", "\n1II");
                    value = value.Replace("n4Bq4", "\n4Bq4");
                    value = value.Replace("nB+PR", "\nB+PR");
                    value = value.Replace("n1Lie", "\n1Lie");
                    value = value.Replace("nA7JO", "\nA7JO");
                    value = value.Replace("n758l", "\n758l");
                    value = value.Replace("n-----END", "\n-----END");
                    
                    // Fix any remaining 'n' followed by base64 characters (more aggressive)
                    value = System.Text.RegularExpressions.Regex.Replace(value, @"n([A-Za-z0-9+/=]{4,})", @"\n$1");
                    
                    // Clean up any remaining invalid characters
                    value = value.Replace("n", ""); // Remove any remaining standalone 'n' characters
                    
                    // Remove any invalid escape sequences that could break JSON BEFORE newline conversion
                    // Use regex to remove all invalid escape sequences (keep only valid ones: \n, \", \\)
                    value = System.Text.RegularExpressions.Regex.Replace(value, @"\\(?![n""\\])", "");
                    
                    // Add proper BEGIN and END markers
                    value = "-----BEGIN PRIVATE KEY-----\n" + value + "\n-----END PRIVATE KEY-----";
                    
                    // Convert actual newlines to escaped newlines for JSON
                    value = value.Replace("\\n", "\n");
                    value = value.Replace("\n", "\\n");
                    
                    // Final cleanup of any invalid escape sequences that might have been created
                    // Use regex to remove all invalid escape sequences (keep only valid ones: \n, \", \\)
                    value = System.Text.RegularExpressions.Regex.Replace(value, @"\\(?![n""\\])", "");
                    
                    Console.WriteLine($"[JSON Repair] Repaired private key length: {value.Length}");
                    Console.WriteLine($"[JSON Repair] Private key ends with: {value.Substring(Math.Max(0, value.Length - 50))}");
                }
                
                data[key] = value;
            }
        }
        
        // Ensure required fields exist
        var requiredFields = new[] { "type", "project_id", "private_key_id", "private_key", "client_email" };
        foreach (var field in requiredFields)
        {
            if (!data.ContainsKey(field))
            {
                Console.WriteLine($"[JSON Repair] Missing required field: {field}");
                return malformedJson; // Return original if missing required fields
            }
        }
        
        // Reconstruct JSON with proper formatting
        var jsonBuilder = new StringBuilder();
        jsonBuilder.AppendLine("{");
        
        var isFirst = true;
        foreach (var kvp in data)
        {
            if (!isFirst) jsonBuilder.AppendLine(",");
            jsonBuilder.Append($"  \"{kvp.Key}\": \"{kvp.Value}\"");
            isFirst = false;
        }
        
        jsonBuilder.AppendLine();
        jsonBuilder.Append("}");
        
        var repairedJson = jsonBuilder.ToString();
        
        // Validate the repaired JSON
        try
        {
            JsonDocument.Parse(repairedJson);
            Console.WriteLine("[JSON Repair] Successfully repaired malformed JSON");
            Console.WriteLine($"[JSON Repair] Repaired JSON preview: {repairedJson.Substring(0, Math.Min(200, repairedJson.Length))}...");
            return repairedJson;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[JSON Repair] Failed to repair JSON: {ex.Message}");
            Console.WriteLine($"[JSON Repair] Repaired JSON that failed validation: {repairedJson}");
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