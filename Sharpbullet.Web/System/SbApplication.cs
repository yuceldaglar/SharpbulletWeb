using SharpBullet.Web.Entities.Core;
using SharpBullet.OAL;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Web;
using System.Web.Configuration;
using SharpBullet.OAL.Metadata;
using System.IO;
using SharpBullet.Web.Entities;
using SharpBullet.Web.System;
using SharpBullet.OAL.Schema;

namespace SharpBullet.Web
{
    public class SbApplication
    {
        private const string SB_APPLICATION = "SbApplication";
        private SbAccess access;
        private string configurationPrefix = "";
        private Dictionary<string, Type> services;

        #region Configuration
        private SbApplicationConfiguration configuration;

        public SbApplicationConfiguration Configuration
        {
            get
            {
                if (configuration == null)
                {
                    configuration = new SbApplicationConfiguration();
                    configuration.Initialize();
                }
                return configuration;
            }
            set { configuration = value; }
        }

        #endregion

        public Dictionary<string, Type> Services
        {
            get
            {
                if (services == null) services = new Dictionary<string, Type>();
                return services;
            }
        }

        


        public static SbApplication Current
        {
            get
            {
                SbApplication app = (SbApplication)HttpContext.Current.Application[SB_APPLICATION];
                if (app == null)
                {
                    app = new SbApplication();
                    HttpContext.Current.Application[SB_APPLICATION] = app;
                }
                return app;
            }
        }

        public SbApplication()
        {           
            this.InitializeOrm(Configuration.DbType, Configuration.Connection);
        }

        public SbAccess Access
        {
            get { return Create<SbAccess>(); }
        }

        public SbMainMenu MainMenu
        {
            get { return Create<SbMainMenu>(); }
        }

        public virtual T Create<T>()
        {
            //TODO check if typename specified in web config
            //TODO cache created object
            return Activator.CreateInstance<T>();
        }

        protected virtual void InitializeOrm(string dbType, string connectionString)
        {
            if (string.IsNullOrEmpty(dbType)
                && string.IsNullOrEmpty(connectionString)) return;

            Transaction.SetConnection(dbType, connectionString);

            if (Configuration.AutoMigration)
            {
                var baseType = typeof(BaseEntity);
                SchemaHelper.Migrate(baseType, true, Transaction.Instance, baseType.Assembly);
            }
        }

        public virtual void Log(SbLogType type, string subclass, string description, string content)
        {
            SbLog log = new SbLog()
            {
                CoreLogType = type,
                Subclass = subclass,
                LogTime = DateTime.Now,
                Description = description,
                Content = content ?? ""
            };
            log.Save();
        }
    }

    public class SbApplicationConfiguration : SbConfiguration
    {
        public string ApplicationTitle { get; set; }
        public string CompanyName { get; set; }


        public bool AutoMigration { get; set; }
        public string DbType { get; set; }
        public string Connection { get; set; }
        public string EntityDll { get; set; }
    }

    public class SbApp
    {
        public static SbAccess Access
        {
            get { return SbApplication.Current.Access; }
        }

        public static T Create<T>()
        {
            return SbApplication.Current.Create<T>();
        }
    }
}