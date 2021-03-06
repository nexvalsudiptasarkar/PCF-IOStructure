using System;
using System.Collections.Generic;
using System.Text;

namespace Amazon.Auth.AccessControlPolicy.ActionIdentifiers
{
    /// <see cref="Amazon.Auth.AccessControlPolicy.Statement.Actions"/>
    [Obsolete(
            "The Amazon.Auth.AccessContolPolicy classes are out of date and are no longer being maintained.\n" +
            "Please construct policy documents using the information found here: https://docs.aws.amazon.com/IAM/latest/UserGuide/access_policies.html#access_policies-json")]
    public static class AppStreamActionIdentifiers
    {
        public static readonly ActionIdentifier AllAppStreamActions = new ActionIdentifier("appstream:*");

        public static readonly ActionIdentifier CreateApplication = new ActionIdentifier("appstream:CreateApplication");
        public static readonly ActionIdentifier CreateSession = new ActionIdentifier("appstream:CreateSession");
        public static readonly ActionIdentifier DeleteApplication = new ActionIdentifier("appstream:DeleteApplication");
        public static readonly ActionIdentifier GetApiRoot = new ActionIdentifier("appstream:GetApiRoot");
        public static readonly ActionIdentifier GetApplication = new ActionIdentifier("appstream:GetApplication");
        public static readonly ActionIdentifier GetApplications = new ActionIdentifier("appstream:GetApplications");
        public static readonly ActionIdentifier GetApplicationError = new ActionIdentifier("appstream:GetApplicationError");
        public static readonly ActionIdentifier GetApplicationErrors = new ActionIdentifier("appstream:GetApplicationErrors");
        public static readonly ActionIdentifier GetApplicationStatus = new ActionIdentifier("appstream:GetApplicationStatus");
        public static readonly ActionIdentifier GetSession = new ActionIdentifier("appstream:GetSession");
        public static readonly ActionIdentifier GetSessions = new ActionIdentifier("appstream:GetSessions");
        public static readonly ActionIdentifier GetSessionStatus = new ActionIdentifier("appstream:GetSessionStatus");
        public static readonly ActionIdentifier UpdateApplication = new ActionIdentifier("appstream:UpdateApplication");
        public static readonly ActionIdentifier UpdateApplicationState = new ActionIdentifier("appstream:UpdateApplicationState");
        public static readonly ActionIdentifier UpdateSessionState = new ActionIdentifier("appstream:UpdateSessionState");
    }
}

