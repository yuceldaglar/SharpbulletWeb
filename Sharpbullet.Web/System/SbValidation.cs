using SharpBullet.Web.Entities;
using SharpBullet.Web.Entities.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpBullet.Web
{
    public static class SbValidation
    {
        public static void Required(string value, string errorMessage)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ApplicationException(errorMessage);
            }
        }

        public static void Required(BaseEntityTenant value, string errorMessage)
        {
            if (value == null || value.NotExist())
            {
                throw new ApplicationException(errorMessage);
            }
        }

        public static void Throw(bool condition, string errorMessage)
        {
            if (condition)
            {
                throw new ApplicationException(errorMessage);
            }
        }
    }
}