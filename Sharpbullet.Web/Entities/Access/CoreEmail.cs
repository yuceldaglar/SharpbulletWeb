using System;
using System.Runtime.CompilerServices;

namespace SharpBullet.Web.Entities.Core
{
	public class CoreEmail : BaseEntityTenant
	{
		public string CredentialPwd
		{
			get;
			set;
		}

		public string CredentialUser
		{
			get;
			set;
		}

		public string DisplayName
		{
			get;
			set;
		}

		public string FromEmail
		{
			get;
			set;
		}

		public string HostName
		{
			get;
			set;
		}

		public int Port
		{
			get;
			set;
		}

		public CoreEmail()
		{
		}
	}
}