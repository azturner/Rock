﻿using System;
using System.Configuration;

namespace Rock.Data
{
    /// <summary>
    /// Static class to support factory method implementation.
    /// </summary>
    public class RepositoryFactory<T> where T : Model<T>
    {
        /// <summary>
        /// Finds a repository object based on app settings in web/app.config file.
        /// </summary>
        /// <typeparam name="T">Model of type T</typeparam>
        /// <returns>IRepository of type T</returns>
        public IRepository<T> FindRepository()
        {
            var type = typeof ( T );
            var key = type.ToString() + "RepositoryType";
            key = key.Substring( key.LastIndexOf( '.' ) + 1 );
            // Check <appSettings> in .config file first...
            var repositoryType = ConfigurationManager.AppSettings[key];

            // If empty, return a default of the EFRepository<T>...
            if (string.IsNullOrEmpty(repositoryType))
            {
                return new EFRepository<T>();
            }

            var settingArray = repositoryType.Split( new[] { ',' } );
            var className = settingArray[0] + "," + settingArray[1];
            var assemblyName = settingArray[2];

            return  (IRepository<T>) Activator.CreateInstance( assemblyName, className ).Unwrap();
        }
    }
}