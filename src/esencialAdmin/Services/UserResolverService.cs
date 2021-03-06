﻿using Microsoft.AspNetCore.Http;

namespace esencialAdmin.Services
{
    public class UserResolverService : IUserResolverService
    {
        private readonly IHttpContextAccessor _context;
        public UserResolverService(IHttpContextAccessor context)
        {
            _context = context;
        }

        public string GetUser()
        {
            return _context?.HttpContext?.User?.Identity?.Name;
        }
    }
}
