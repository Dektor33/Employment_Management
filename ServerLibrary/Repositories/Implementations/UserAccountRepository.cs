using BaseLibrary.DTOs;
using BaseLibrary.Entities;
using BaseLibrary.Responses;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ServerLibrary.Data;
using ServerLibrary.Helpers;
using ServerLibrary.Repositories.Contracts;
using BCrypt.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;


namespace ServerLibrary.Repositories.Implementations
{
    public class UserAccountRepository(IOptions<JwtSection> config, AppDbContext appDbContext) : IUserAccount
    {
        public async Task<GeneralResponse> CreateAsync(Register user)
        {
            if (user is null) { return new GeneralResponse(false, "Model is Empty."); }
            var checkUser = await FindUserByEmail(user.Email!);
            if (checkUser != null) return new GeneralResponse(true, "User registered already.");

            //Save user
            var applicationUser = await AddToDatabase(new ApplicationUser() 
                { Fullname = user.Fullname, 
                  Email = user.Email,
                  Password = BCrypt.Net.BCrypt.HashPassword(user.Password)
                });

            // check, create and assign role
            var checkAdminRole = await appDbContext.SystemRoles.FirstOrDefaultAsync(_ => _.Name!.Equals(Constants.Admin));
            if(checkAdminRole is null)
            {
                var createAdminRole = await AddToDatabase(new SystemRole() { Name = Constants.Admin });
                await AddToDatabase(new UserRole() { RoleId = createAdminRole.Id, UserId = applicationUser.Id });
                return new GeneralResponse(true, "Account created");
            }

            var checkUserRole = await appDbContext.SystemRoles.FirstOrDefaultAsync(_ => _.Name!.Equals(Constants.User));
            if (checkUserRole is null)
            {
                var response = await AddToDatabase(new SystemRole() { Name = Constants.User });
                await AddToDatabase(new UserRole() { RoleId = response.Id, UserId = applicationUser.Id });
            }
            else
            {
                await AddToDatabase(new UserRole() { RoleId = checkUserRole.Id, UserId = applicationUser.Id });
            }

            return new GeneralResponse(true, "Account created");

        }

        public async Task<LoginResponse> SignInAsync(Login user)
        {
            if (user is null) return new LoginResponse(false, "Model is empty");

            var applicationUser = await FindUserByEmail(user.Email!);
            if (applicationUser is null) return new LoginResponse(false, "User not found");

            //Verify password
            if (!BCrypt.Net.BCrypt.Verify(user.Password, applicationUser.Password))
                return new LoginResponse(false, "Email/Password not valid");

            var getUserRole = await FindUserRole(applicationUser.Id);
            if (getUserRole is null) return new LoginResponse(false, "user role not found");

            var getRoleName = await FindRoleName(getUserRole.RoleId);
            if (getRoleName is null) return new LoginResponse(false, "user role not found");

            string jwtToken = GenerateToken(applicationUser, getRoleName!.Name!);
            string refreshToken = GenerateRefreshToken();

            //Save the Refresh token to the database
            var findUser = await appDbContext.RefreshTokenInfos.FirstOrDefaultAsync(_ => _.UserId == applicationUser.Id);
            if (findUser is not null)
            {
                findUser!.Token = refreshToken;
                await appDbContext.SaveChangesAsync();
            }
            else
            {
                await AddToDatabase(new RefreshTokenInfo() { Token = refreshToken, UserId = applicationUser.Id });
            }
            return new LoginResponse(true,"login successfully", jwtToken, refreshToken);
        }

        private string GenerateToken(ApplicationUser user, string role)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config.Value.Key!));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
            var userClaims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Fullname!),
                new Claim(ClaimTypes.Email, user.Email!),
                new Claim(ClaimTypes.Role, role!)
            };
            var token = new JwtSecurityToken(
                issuer: config.Value.Issuer,
                audience: config.Value.Audience,
                claims: userClaims,
                expires: DateTime.Now.AddSeconds(2),
                signingCredentials: credentials
                );
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private async Task<UserRole> FindUserRole(int userId) => await appDbContext.UserRoles.FirstOrDefaultAsync(_ => _.UserId == userId);
        private async Task<SystemRole> FindRoleName(int roleId) => await appDbContext.SystemRoles.FirstOrDefaultAsync(_ => _.Id == roleId);
        private static string GenerateRefreshToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

        private async Task<ApplicationUser> FindUserByEmail(string email)=>
            await appDbContext.ApplicationUsers.FirstOrDefaultAsync(_=>_.Email!.ToLower()!.Equals(email!.ToLower()));

        private async Task<T> AddToDatabase<T> (T model)
        {
            var result = appDbContext.Add(model!);
            await appDbContext.SaveChangesAsync();
            return(T) result.Entity;
        }

        public async Task<LoginResponse> RefreshTokenAsync(RefreshToken token)
        {
            if(token is null) return new LoginResponse(false, "Model is Empty");

            var findToken = await appDbContext.RefreshTokenInfos.FirstOrDefaultAsync(_ => _.Token!.Equals(token.Token));
            if (findToken is null) return new LoginResponse(false, "Refresh token is required");

            //get user details
            var user = await appDbContext.ApplicationUsers.FirstOrDefaultAsync(_ => _.Id == findToken.UserId);
            if (user is null) return new LoginResponse(false, "Refresh token could not be generated because user not found");

            var userRole = await FindUserRole(user.Id);
            var roleName = await FindRoleName(userRole.RoleId);
            string jwtToken = GenerateToken(user, roleName.Name);
            string refreshToken = GenerateRefreshToken();

            var updateRefreshToken = await appDbContext.RefreshTokenInfos.FirstOrDefaultAsync(_ => _.UserId == user.Id);
            if (updateRefreshToken is null) return new LoginResponse(false, "Refresh token could not be generated because user has not signed in");

            updateRefreshToken.Token = refreshToken;
            await appDbContext.SaveChangesAsync();
            return new LoginResponse(true, "Token refreshed successfully", jwtToken, refreshToken);

        }

        public async Task<List<ManageUser>> GetUsers()
        {
            var allUsers = await GetApplicationUsers();
            var allUserRoles = await UserRoles();
            var allRoles = await SystemRoles();

            if (allUsers.Count == 0 || allRoles.Count == 0) return null!;

            var users = new List<ManageUser>();
            foreach (var user in allUsers)
            {
                var userRole = allUserRoles.FirstOrDefault(u => u.UserId == user.Id);
                var roleName = allRoles.FirstOrDefault(r => r.Id == userRole!.RoleId);
                users.Add(new ManageUser() { UserId = user.Id, Name = user.Fullname!, Email = user.Email!, Role = roleName!.Name! });
            }
            return users;
        }


        public async Task<GeneralResponse> UpdateUser(ManageUser user)
        {
            var getRole = (await SystemRoles()).FirstOrDefault(r => r.Name!.Equals(user.Role));
            var userRole = await appDbContext.UserRoles.FirstOrDefaultAsync(u => u.UserId == user.UserId);
            userRole!.RoleId = getRole!.Id;
            await appDbContext.SaveChangesAsync();
            return new GeneralResponse(true, "User role updated successfully");
        }


        public async Task<List<SystemRole>> GetRoles() => await SystemRoles();


        public async Task<GeneralResponse> DeleteUser(int id)
        {
            var user = await appDbContext.ApplicationUsers.FirstOrDefaultAsync(u => u.Id == id);
            appDbContext.ApplicationUsers.Remove(user!);
            await appDbContext.SaveChangesAsync();
            return new GeneralResponse(true, "User successfully deleted");
        }


        private async Task<List<SystemRole>> SystemRoles() => await appDbContext.SystemRoles.AsNoTracking().ToListAsync();
        private async Task<List<UserRole>> UserRoles() => await appDbContext.UserRoles.AsNoTracking().ToListAsync();
        private async Task<List<ApplicationUser>> GetApplicationUsers() => await appDbContext.ApplicationUsers.AsNoTracking().ToListAsync();

        public async Task<string> GetUserImage(int id) => (await GetApplicationUsers()).FirstOrDefault(u => u.Id == id)!.Image;
        public async Task<bool> UpdateProfile(UserProfile profile)
        {
            var user = await appDbContext.ApplicationUsers.FirstOrDefaultAsync(u => u.Id == int.Parse(profile.Id));
            user!.Email = profile.Email;
            user.Fullname = profile.Name;
            user.Image = profile.Image;
            await appDbContext.SaveChangesAsync();
            return true;
        }
    }
}
