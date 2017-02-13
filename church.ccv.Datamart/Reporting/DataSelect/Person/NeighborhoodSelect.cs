﻿using System;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Web.UI.WebControls;
using church.ccv.Datamart.Model;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Reporting;

namespace church.ccv.Datamart.Reporting.DataSelect.Person
{
    /// <summary>
    /// 
    /// </summary>
    [Description( "Select the Neighborhood of the Person" )]
    [Export( typeof( DataSelectComponent ) )]
    [ExportMetadata( "ComponentName", "Select Person's Neighborhood" )]

    [BooleanField( "Show As Link", "", true)]
    public class NeighborhoodSelect : DataSelectComponent
    {
        /// <summary>
        /// Gets the name of the entity type. Filter should be an empty string
        /// if it applies to all entities
        /// </summary>
        /// <value>
        /// The name of the entity type.
        /// </value>
        public override string AppliesToEntityType
        {
            get
            {
                return typeof( Rock.Model.Person ).FullName;
            }
        }

        /// <summary>
        /// Gets the section that this will appear in in the Field Selector
        /// </summary>
        /// <value>
        /// The section.
        /// </value>
        public override string Section
        {
            get
            {
                return "Datamart";
            }
        }

        /// <summary>
        /// The PropertyName of the property in the anonymous class returned by the SelectExpression
        /// </summary>
        /// <value>
        /// The name of the column property.
        /// </value>
        public override string ColumnPropertyName
        {
            get
            {
                return "Neighborhood";
            }
        }

        /// <summary>
        /// Gets the type of the column field.
        /// </summary>
        /// <value>
        /// The type of the column field.
        /// </value>
        public override Type ColumnFieldType
        {
            get { return typeof( string ); }
        }

        /// <summary>
        /// Gets the default column header text.
        /// </summary>
        /// <value>
        /// The default column header text.
        /// </value>
        public override string ColumnHeaderText
        {
            get
            {
                return "Neighborhood";
            }
        }

        /// <summary>
        /// Gets the title.
        /// </summary>
        /// <param name="entityType"></param>
        /// <returns></returns>
        /// <value>
        /// The title.
        /// </value>
        public override string GetTitle( Type entityType )
        {
            return "Neighborhood";
        }

        /// <summary>
        /// Gets the grid field.
        /// </summary>
        /// <param name="entityType">Type of the entity.</param>
        /// <param name="selection">The selection.</param>
        /// <returns></returns>
        public override System.Web.UI.WebControls.DataControlField GetGridField( Type entityType, string selection )
        {
            var result = new BoundField();
            result.HtmlEncode = false;
            return result;
        }

        /// <summary>
        /// Gets the expression.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="entityIdProperty">The entity identifier property.</param>
        /// <param name="selection">The selection.</param>
        /// <returns></returns>
        public override System.Linq.Expressions.Expression GetExpression( Rock.Data.RockContext context, System.Linq.Expressions.MemberExpression entityIdProperty, string selection )
        {
            bool showAsLink = this.GetAttributeValueFromSelection( "ShowAsLink", selection ).AsBooleanOrNull() ?? false;
            var datamartNeighborhoodsService = new Service<DatamartNeighborhood>( context );
            var datamartPersonService = new Service<DatamartPerson>( context );
            var personService = new PersonService( context );

            var qryDatamartNeighborhoods = datamartNeighborhoodsService.Queryable();
            var qryDatamartPerson = datamartPersonService.Queryable();
            var qryPerson = personService.Queryable();

            IQueryable<string> groupLinkQuery;

            string baseGroupUrl = System.Web.VirtualPathUtility.ToAbsolute( "~/Group/" );

            if ( showAsLink )
            {
                // include Neighboorhood name as a comment so that sorting works
                groupLinkQuery = qryPerson
                    .Select( p => qryDatamartPerson.Where( d => d.PersonId == p.Id )
                        .Select( s => qryDatamartNeighborhoods.Where( n => n.NeighborhoodId == s.NeighborhoodId )
                            .Select( ss => "<!-- " + ss.NeighborhoodName + "--><a href='" + baseGroupUrl + ss.NeighborhoodId.ToString() + "'>" + ss.NeighborhoodName + "</a>" )
                            .FirstOrDefault() ).FirstOrDefault() );
            }
            else
            {
                groupLinkQuery = qryPerson
                    .Select( p => qryDatamartPerson.Where( d => d.PersonId == p.Id )
                        .Select( s => qryDatamartNeighborhoods.Where( n => n.NeighborhoodId == s.NeighborhoodId )
                            .Select( a => a.NeighborhoodName )
                            .FirstOrDefault() ).FirstOrDefault() );
            }

            var resultExpression = SelectExpressionExtractor.Extract( groupLinkQuery, entityIdProperty, "p" );

            return resultExpression;
        }
    }
}