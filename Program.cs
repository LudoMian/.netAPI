using System.Text;
using DotnetAPI.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args); //inject IConfiguration into all of our controllers by default.

builder.Services.AddControllers(); // add endpoints to our controller reference

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//CORS-crossOriginResourceSharing 
builder.Services.AddCors((options) =>
    {
        options.AddPolicy("DevCors", (corsBuilder) =>
        {
            corsBuilder.WithOrigins("http://localhost:4200", " http://localhost:3000", " http://localhost:8000") //angular p:4200, react p:3000, view p:8000 those are main singlepage app frameworks
                .AllowAnyMethod() // method like GET,POST,PUT,DELETE requests -to be able to call any method that its create inside controller
                .AllowAnyHeader() // in case we wanna write custom headers that we wanna access in API and get from Frontend
                .AllowCredentials(); // to get in Cookies and Authentication and all kinds of other stuff           
        });
        options.AddPolicy("ProdCors", (corsBuilder) =>
        {
            corsBuilder.WithOrigins("http://myProductionSite.com")
                .AllowAnyMethod() 
                .AllowAnyHeader() 
                .AllowCredentials(); 
        });
    }); 
    
string? tokenKeyString = builder.Configuration.GetSection("AppSettings:TokenKey").Value;

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>{
        options.TokenValidationParameters = new TokenValidationParameters()
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                    tokenKeyString != null ? tokenKeyString : ""
                )),
                ValidateIssuer = false,
                ValidateAudience = false
            };
        });


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseCors("DevCors");
}
else    // we don't use https in development ..so to be use in published API
{
    app.UseHttpsRedirection();
    app.UseCors("ProdCors");
}

app.UseAuthentication();    //has to be before Authorization

app.UseAuthorization();

app.MapControllers(); // have access to our Controller mapping and will be able to setup Routes for us

app.Run();
