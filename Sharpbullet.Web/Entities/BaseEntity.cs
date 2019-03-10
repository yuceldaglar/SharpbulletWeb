using SharpBullet.Entities;
using System;

namespace SharpBullet.Web.Entities
{
    public abstract class BaseEntity : SbEntity
    {
        public static DateTime MinDate = new DateTime(1753, 1, 1, 0, 0, 0, 0);

        public static void Required(BaseEntity entity, string errorMessage)
        {
            if (entity.NotExist())
                throw new Exception(errorMessage);
        }
        
    }

    public abstract class BaseEntityTenant : BaseEntity
    {      
        /// <summary>
        /// Multi-tenant uygulamalar için organizasyon key değeri
        /// </summary>
        public int TenantId { get; set; }






        public override void Validate()
        {
            if (TenantId <= 0)
                throw new Exception(SbText.Instance.TenantParameterMissing());

            base.Validate();
        }
    }

    public class BaseEntityDeletable : BaseEntity
    {
        public bool Deleted { get; set; }
    }

    public class BaseEntityTenantDeletable : BaseEntityTenant
    {
        public bool Deleted { get; set; }
    }
}