﻿using Authentication_System.Model;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Authentication_System.DataAccessLayer
{
    //User registration and login Data Access Layer
    public class AuthenticationDataAccess : IAuthenticationDataAccess
    {
        private readonly IConfiguration _configuration;
        private readonly MongoClient _mongoConnection;
        private readonly IMongoCollection<RegisterUserRequest> _booksCollection;

        //This method use to create a DB Connection
        public AuthenticationDataAccess(IConfiguration configuration)
        {
            _configuration = configuration;
            _mongoConnection = new MongoClient(_configuration["BookStoreDatabase:ConnectionString"]);
            var MongoDataBase = _mongoConnection.GetDatabase(_configuration["BookStoreDatabase:DatabaseName"]);
            _booksCollection = MongoDataBase.GetCollection<RegisterUserRequest>(_configuration["BookStoreDatabase:BooksCollectionName"]);
        }

        //This method use to user registration
        public async Task<RegisterUserResponse> RegisterUser(RegisterUserRequest request)
        {

            RegisterUserResponse response = new RegisterUserResponse();

            try
            {
                byte[] encData_byte = new byte[request.Password.Length];
                encData_byte = System.Text.Encoding.UTF8.GetBytes(request.Password);
                string encriptedPassword = Convert.ToBase64String(encData_byte);

                request.Password = encriptedPassword;

                var res = await _booksCollection.Find(x => x.NIC == request.NIC).ToListAsync();

                if (res.Count == 0)
                {
                    _booksCollection.InsertOneAsync(request);
                    response.IsSuccess = true;
                    response.Message = "Successfull Registration";
                }
                else
                {
                    response.IsSuccess = true;
                    response.Message = "User Already Registrated";
                }
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = "Exception Occurs : " + ex.Message;
            }

            return response;

        }

        //This method use to user Login
        public async Task<UserLoginResponse> UserLogin(UserLoginRequest request)
        {
            UserLoginResponse response = new UserLoginResponse();
            

            try
            {
                response.data = new List<RegisterUserRequest>();

                response.data = await _booksCollection.Find(x => x.UserName == request.UserName).ToListAsync();

                byte[] encData_byte = new byte[request.Password.Length];
                encData_byte = System.Text.Encoding.UTF8.GetBytes(request.Password);
                string encriptedPassword = Convert.ToBase64String(encData_byte);
                //var res1 = response.data[0].IsActive;

                if (response.data[0].IsActive == true)
                {
                    response.data = await _booksCollection.Find(x => x.UserName == request.UserName && x.Password == encriptedPassword).ToListAsync();

                    if (response.data == null || response.data.Count == 0)
                    {
                        response.IsSuccess = true;
                        response.Message = "Username or password Incorrect";
                        response.data = null;
                    }
                    else
                    {
                        response.IsSuccess = true;
                        response.Message = "SuccessFull";

                        response.Token = GenerateJWT(request.UserName);
                    }
                }
                else
                {
                    response.IsSuccess = true;
                    response.Message = "User InActive";
                    response.data = null;
                }
  
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = "Exception Occurs : " + ex.Message;
            }

            return response;

        }

        //This method use to get a JWT tocken
        public string GenerateJWT(string Username)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            //claim is used to add identity to JWT token
            var claims = new[] {
             new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
             new Claim(JwtRegisteredClaimNames.Sid, Username),
             //new Claim(JwtRegisteredClaimNames.Email, Password),
             new Claim("Date", DateTime.Now.ToString()),
             };

            var token = new JwtSecurityToken(_configuration["Jwt:Issuer"],
              _configuration["Jwt:Audiance"],
              claims,    //null original value
              expires: DateTime.Now.AddMinutes(120),

              //notBefore:
              signingCredentials: credentials);

            string Data = new JwtSecurityTokenHandler().WriteToken(token); //return access token 
            return Data;
        }
    }
}
