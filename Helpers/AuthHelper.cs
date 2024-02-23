using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Dapper;
using DotnetAPI.Data;
using DotnetAPI.Dtos;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;

namespace DotnetAPI.Helpers
{
    public class AuthHelper
    {
        private readonly DataContextDapper _dapper;
        private readonly IConfiguration _config;
        public AuthHelper(IConfiguration config)
        {
            _dapper = new DataContextDapper(config);
            _config = config;   //private field == to pass in config
        }
        public byte[] GetPasswordHash(string passWord, byte[] passwordSalt)
        {
            string passwordSaltPlusString = _config.GetSection("AppSettings:PasswordKey").Value +
                Convert.ToBase64String(passwordSalt);   // ToBase64String so it's properlly converted

            // returning: byte[] passwordHash =
            return KeyDerivation.Pbkdf2(
                password: passWord,
                salt: Encoding.ASCII.GetBytes(passwordSaltPlusString),
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 100000,
                numBytesRequested: 256 / 8
                );
        }

        public string CreateToken(int userId)
        {
            // to identify user
            Claim[] claims = new Claim[]{
                new Claim("userId", userId.ToString())
            };

            // // SymmetricSecurityKey(byte[]) so we need to convert             
            // SymmetricSecurityKey tokenKey = new SymmetricSecurityKey
            //         (Encoding.UTF8.GetBytes(
            //             _config.GetSection("Appsettings:TokenKey").Value
            //         )
            //     );

            // possible null value for parameter s in byte[] Encoding.GetBytes(string s)
            string? tokenKeyString = _config.GetSection("AppSettings:TokenKey").Value;
            Console.WriteLine(tokenKeyString);
            Console.WriteLine(Encoding.UTF8.GetBytes(tokenKeyString != null ? tokenKeyString: ""));

            SymmetricSecurityKey tokenKey = new SymmetricSecurityKey
                    (Encoding.UTF8.GetBytes(
                        tokenKeyString != null ? tokenKeyString : ""
                    )
                );

            SigningCredentials credentials = new SigningCredentials(
                    tokenKey,
                    SecurityAlgorithms.HmacSha512Signature
                );

            // Creating Token ..instead of passing s.t. (args) to it, we describe a property of Security_Token_Descriptor
            SecurityTokenDescriptor descriptor = new SecurityTokenDescriptor()
            {
                // object declaration
                Subject = new ClaimsIdentity(claims),
                SigningCredentials = credentials,
                Expires = DateTime.Now.AddDays(1)
            };

            // now we need set up Token Handler which is gonna use this discriptor
            // storing Token
            JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler(); //JwtSecurityTokenHandler it's just a class that got some methods we use to store our token

            SecurityToken token = tokenHandler.CreateToken(descriptor);     //this is our actual Token

            // We need to convert our token to string so we have universal format  
            return tokenHandler.WriteToken(token);
        }
        public bool SetPassword(UserForLoginDto userForSetPassword)
        {            
            // setup passwordSalt
            byte[] passwordSalt = new byte[128 / 8]; // byte array of 128bit
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())  // added -using System.Security.Cryptography
            {
                rng.GetNonZeroBytes(passwordSalt);  // get random number and populate it to passwordSalt
            }

            byte[] passwordHash = GetPasswordHash(userForSetPassword.Password, passwordSalt);

            // creating sqlstatement and parameters (inside sql statement creating VAR with @Symbol)
            string sqlAddAuth = @"EXEC TutorialAppSchema.spRegistration_Upsert
                    @Email = @EmailParam, 
                    @PasswordHash = @PasswordHashParam, 
                    @PasswordSalt = @PasswordSaltParam";

            // // creating List of sql parameters with SqlCall, dapper parameter
            // List<SqlParameter> sqlParameters = new List<SqlParameter>();

            // SqlParameter emailParameter = new SqlParameter("@EmailParam", SqlDbType.VarChar);
            // emailParameter.Value = userForSetPassword.Email;
            // sqlParameters.Add(emailParameter);

            // SqlParameter passwordHashParameter = new SqlParameter("@PasswordHashParam", SqlDbType.VarBinary);
            // passwordHashParameter.Value = passwordHash;
            // sqlParameters.Add(passwordHashParameter);

            // SqlParameter passwordSaltParameter = new SqlParameter("@PasswordSaltParam", SqlDbType.VarBinary);
            // passwordSaltParameter.Value = passwordSalt;
            // sqlParameters.Add(passwordSaltParameter);
            
            DynamicParameters sqlParameters = new DynamicParameters();
            sqlParameters.Add("@EmailParam", userForSetPassword.Email, DbType.String);
            sqlParameters.Add("@PasswordHashParam", passwordHash, DbType.Binary);
            sqlParameters.Add("@PasswordSaltParam", passwordSalt, DbType.Binary);
            
            // now passing it to database
            return _dapper.ExecuteSqlWithParameters(sqlAddAuth, sqlParameters);    
            // if ExecuteSqlWithParameters() was succesful return true
        }
    }
}