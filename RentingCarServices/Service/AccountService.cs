﻿using BusinessObjects.Models;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using RentingCarRepositories.RepositoryInterface;
using RentingCarServices.ServiceInterface;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace RentingCarServices.Service
{
    public class AccountService : IAccountService
    {
        private readonly IConfiguration _configuration;
        public readonly IAccountRepository _accountRepository;

        public AccountService(IAccountRepository accountRepository, IConfiguration configuration)
        {
            this._configuration = configuration;
            _accountRepository = accountRepository;
        }

        public void CreateAccount(string email, string username, string password, string confirmPassword)
        {
            if (email != null && username != null && password != null && confirmPassword != null)
            {
                if(confirmPassword.Equals(password)) {
                    // generate a 128-bit salt using a cryptographically strong random sequence of nonzero values
                    byte[] salt = new byte[128 / 8];
                    using (var rngCsp = new RNGCryptoServiceProvider())
                    {
                        rngCsp.GetNonZeroBytes(salt);
                    }
                    // derive a 256-bit subkey (use HMACSHA256 with 100,000 iterations)
                    string hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                        password: password,
                        salt: salt,
                        prf: KeyDerivationPrf.HMACSHA512,
                        iterationCount: 100000,
                        numBytesRequested: 512 / 8));
                    Account account = new Account
                    {
                        Email = email,
                        UserName = username,
                        Password = hashed,
                        RoleId = 1,
                        StatusId = 1,

                    };
                    _accountRepository.CreateAccount(account);
                }
                else
                {
                    throw new InvalidOperationException("Password and Confirm Password does not match!");
                }

                
            }
        }

        public bool DeleteAccountByEmail(string email)
        {
            Account account = _accountRepository.GetAccountByEmail(email);
            if (account == null)
            {
                return false;
            }
            bool checkDelete = _accountRepository.DeleteAccount(account);
            return checkDelete;
        }

        public Account GetAccountByEmail(string email)
        {
            Account account = _accountRepository.GetAccountByEmail(email);
            if (account != null)
            {
                return account;
            }
            return null;
        }

        public List<Account> GetAllAccounts()
        {
            return _accountRepository.GetAllAccounts();
        }


        private const string TokenSecret = "RentingCarEXE201";
        private static readonly TimeSpan TokenLifeTime = TimeSpan.FromMinutes(30);
        public string SignIn(string email, string password)
        {
            var existedUser = _accountRepository.GetAccountByEmail(email);

            if (existedUser != null && existedUser.Password.Equals(password))
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(TokenSecret);

                var claims = new List<Claim>
                {
                    new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    new(JwtRegisteredClaimNames.Sub, existedUser.Email.Trim()),
                    new(JwtRegisteredClaimNames.Email, existedUser.Email.Trim()),
                    new(ClaimTypes.Name, existedUser.UserName),
                    new(ClaimTypes.Role, existedUser.Role.RoleName.Trim()),
                };

                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(claims),
                    Expires = DateTime.UtcNow.Add(TokenLifeTime),
                    Issuer = "http://localhost:5063",
                    Audience = "User",
                    SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key),
                        SecurityAlgorithms.HmacSha256Signature),
                };

                var token = tokenHandler.CreateToken(tokenDescriptor);
                var jwt = tokenHandler.WriteToken(token);
                return jwt;
            }

            return string.Empty;
        }
    }
}
