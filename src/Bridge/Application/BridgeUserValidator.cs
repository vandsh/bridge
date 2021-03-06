﻿using CMS.Membership;
using Nancy.Authentication.Basic;
using System.Security.Claims;
using System.Security.Principal;

namespace Bridge.Application
{
    public class BridgeUserValidator : IUserValidator
    {
        public ClaimsPrincipal Validate(string username, string password)
        {
            ClaimsPrincipal retval = null;
            var defaultSite = BridgeConfiguration.GetConfig().DefaultSite;
            var authUser = AuthenticationHelper.AuthenticateUser(username, password, defaultSite, false);
            if(authUser != null)
            {
                retval = new ClaimsPrincipal(new GenericIdentity(username));
            }
            return retval;
        }
    }
}