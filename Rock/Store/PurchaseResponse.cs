﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rock.Store
{
    /// <summary>
    /// Represents a store category for packages.
    /// </summary>
    public class PurchaseResponse : StoreModel
    {
        /// <summary>
        /// Gets or sets the result of the purchase. 
        /// </summary>
        /// <value>
        /// A <see cref="System.Enum"/> representing the result of the purchase.
        /// </value>
        public PurchaseResult PurchaseResult { get; set; }

        /// <summary>
        /// Gets or sets the message of the purchase result. 
        /// </summary>
        /// <value>
        /// A <see cref="System.String"/> representing message of the purchase result.
        /// </value>
        public string Message { get; set; }

        /// <summary>
        /// Gets or sets the Id of the Package. 
        /// </summary>
        /// <value>
        /// A <see cref="System.Int32"/> representing the Id of the Package.
        /// </value>
        public int PackageId { get; set; }

        /// <summary>
        /// Gets or sets the Name of the Package. 
        /// </summary>
        /// <value>
        /// A <see cref="System.String"/> representing the Name of the Package.
        /// </value>
        public string PackageName { get; set; }

        /// <summary>
        /// Gets or sets the Id of the Vendor. 
        /// </summary>
        /// <value>
        /// A <see cref="System.Int32"/> representing the Id of the Vendor.
        /// </value>
        public int VendorId { get; set; }

        /// <summary>
        /// Gets or sets the Name of the Vendor. 
        /// </summary>
        /// <value>
        /// A <see cref="System.String"/> representing the Name of the Vendor.
        /// </value>
        public string VendorName { get; set; }

        /// <summary>
        /// Gets or sets the Name of the person who installed the package. 
        /// </summary>
        /// <value>
        /// A <see cref="System.String"/> representing the Name of the installer.
        /// </value>
        public string InstalledBy { get; set; }

        /// <summary>
        /// Gets or sets the package install steps.
        /// </summary>
        /// <value>
        /// The package install steps.
        /// </value>
        public List<PackageInstallStep> PackageInstallSteps { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    public enum PurchaseResult
    {
        /// <summary>
        /// The success
        /// </summary>
        Success,

        /// <summary>
        /// The authenication failed
        /// </summary>
        AuthenicationFailed,

        /// <summary>
        /// The not authorized
        /// </summary>
        NotAuthorized,

        /// <summary>
        /// The no card on file
        /// </summary>
        NoCardOnFile,

        /// <summary>
        /// The payment failed
        /// </summary>
        PaymentFailed,

        /// <summary>
        /// The error
        /// </summary>
        Error
    }
}
