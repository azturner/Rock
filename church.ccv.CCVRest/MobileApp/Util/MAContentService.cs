﻿using System;
using System.Collections.Generic;
using System.Data.Entity.Spatial;
using System.Linq;
using church.ccv.CCVRest.MobileApp.Model;
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;

namespace church.ccv.CCVRest.MobileApp
{
    public class MAContentService
    {
        public static int? GetNearestCampus( double longitude, double latitude, double maxDistanceMeters )
        {
            List<CampusCache> campusCacheList = CampusCache.All( false );

            // assume we're too far away from any campuses
            double closestDistance = maxDistanceMeters;
            int? closestCampusId = null;

            // take the provided long/lat and get a geoPoint out of it
            DbGeography geoPoint = DbGeography.FromText( string.Format( "POINT({0} {1})", longitude, latitude ) );

            // now go thru each campus
            foreach ( CampusCache campusCache in campusCacheList )
            {
                // if the campus has a geopoint defined
                if ( campusCache.Location.Longitude.HasValue && campusCache.Location.Latitude.HasValue )
                {
                    // put it in a geoPoint
                    DbGeography campusGeoPoint = DbGeography.FromText( string.Format( "POINT({0} {1})", campusCache.Location.Longitude, campusCache.Location.Latitude ) );

                    // take the distance between the the provided point and this campus
                    double? distanceFromCampus = campusGeoPoint.Distance( geoPoint );

                    // if a calcluation could be performed and it's closer than what we've already found, take it.
                    if ( distanceFromCampus.HasValue && distanceFromCampus < closestDistance )
                    {
                        closestDistance = distanceFromCampus.Value;
                        closestCampusId = campusCache.Id;
                    }
                }
            }

            // return whatever the closest campus was (or null if none were close enough)
            return closestCampusId;
        }

        public static KidsContentModel BuildKidsContent( Person person )
        {
            // define our constants (for gradeToFamilyMap, there's nothing in Rock
            // that actually maps a Grade to Content. It's just done based on the room
            //  a kid checks in to, so define that mapping here)
            const string MAMyFamilyContentOverrideKey = "MobileAppKidContentOverride";
            const string MAMyFamilyContentLevelKey = "MobileAppKidContentLevel";

            Dictionary<int, string> gradeToFamilyMap = new Dictionary<int, string>();
            gradeToFamilyMap.Add( 0, "Infants" );
            gradeToFamilyMap.Add( 1, "Early Kids" );
            gradeToFamilyMap.Add( 5, "Later Kids" );
            gradeToFamilyMap.Add( 7, "Junior High" );
            gradeToFamilyMap.Add( 9, "High School" );

            // see if the person has an override that sets their 
            // content level (Common among people with Special Needs)
            person.LoadAttributes();
            string targetContent = person.AttributeValues[MAMyFamilyContentOverrideKey]?.ToString();

            // if blank, there's no override, so choose content based on their grade/age
            if( string.IsNullOrWhiteSpace( targetContent ) == true )
            {
                // this is technically cheating, but Rock abstracts grade and doesn't natively
                // know about the US standard. To simplify things, let's do the conversion here
                int realGrade = 0; //(assume infant / pre-k)
                if ( person.GradeOffset.HasValue )
                {
                    realGrade = 12 - person.GradeOffset.Value;
                }
                else
                {
                    // no grade, so try using their age
                    if ( person.Age.HasValue )
                    {
                        if ( person.Age >= 14 )
                        {
                            realGrade = 9;
                        }
                        else if ( person.Age >= 12 )
                        {
                            realGrade = 7;
                        }
                        else if ( person.Age >= 10 )
                        {
                            realGrade = 5;
                        }
                        else if ( person.Age >= 6 )
                        {
                            realGrade = 1;
                        }
                        else
                        {
                            realGrade = 0;
                        }
                    }
                }

                // now whether from their actual grade, or inferred by age, get
                // the content
                targetContent = gradeToFamilyMap[realGrade];
            }

            // now that we know the range, build the content channel queries
            RockContext rockContext = new RockContext();
            ContentChannelService contentChannelService = new ContentChannelService( rockContext );

            // first, get AtCCV
            const int ContentChannelId_AtCCV = 286;
            ContentChannel atCCV = contentChannelService.Get( ContentChannelId_AtCCV );

            // sort by date
            var atCCVItems = atCCV.Items.OrderByDescending( i => i.CreatedDateTime );

            // now take the first one that matches our grade offset.

            // while iterating over these in memory could become slow as the list grows, the business
            // requirements of CCV mean it won't. Because there will always be a new entry each week for each grade level,
            // it won't ever realistically go over the first 4 items
            ContentChannelItem atCCVItem = null;
            foreach ( var item in atCCVItems )
            {
                // this is the slow part. If it ever does become an issue, replace it with an AV table join.
                item.LoadAttributes();
                if ( item.AttributeValues[MAMyFamilyContentLevelKey].ToString() == targetContent )
                {
                    atCCVItem = item;
                    break;
                }
            }


            // next, get Faith Building At Home
            const int ContentChannelId_FaithBuilding = 287;
            ContentChannel faithBuilding = contentChannelService.Get( ContentChannelId_FaithBuilding );

            // sort by date
            var faithBuildingItems = faithBuilding.Items.OrderByDescending( i => i.CreatedDateTime );

            // as above, we'll iterate over the whole list in memory, knowing we'll actually only load attributes for about 4 items.
            ContentChannelItem faithBuildingItem = null;
            foreach ( var item in faithBuildingItems )
            {
                item.LoadAttributes();
                if ( item.AttributeValues[MAMyFamilyContentLevelKey].ToString() == targetContent )
                {
                    faithBuildingItem = item;
                    break;
                }
            }


            // finally, get the resources available for the grade level
            const int ContentChannelId_Resources = 288;
            ContentChannel resourceChannel = contentChannelService.Get( ContentChannelId_Resources );

            List<ContentChannelItem> resourceList = new List<ContentChannelItem>();
            foreach ( var item in resourceChannel.Items )
            {
                item.LoadAttributes();
                if ( item.AttributeValues[MAMyFamilyContentLevelKey].ToString().Contains( targetContent ) )
                {
                    resourceList.Add( item );
                }
            }

            // sort the resource list by priority
            resourceList.Sort( delegate ( ContentChannelItem a, ContentChannelItem b )
            {
                if ( a.Priority < b.Priority )
                {
                    return -1;
                }
                else if ( a.Priority == b.Priority )
                {
                    return 0;
                }
                else
                {
                    return 1;
                }
            } );


            // prepare our model - we'll require both main category items 
            // and otherwise return failure (note that resources CAN be empty)
            if ( atCCVItem != null && faithBuildingItem != null )
            {
                string publicAppRoot = GlobalAttributesCache.Value( "PublicApplicationRoot" ).EnsureTrailingForwardslash();

                KidsContentModel contentModel = new KidsContentModel();

                // At CCV
                contentModel.AtCCV_Title = atCCVItem.AttributeValues["AtCCVTitle"].ToString();
                contentModel.AtCCV_Content = atCCVItem.AttributeValues["AtCCVContent"].ToString();
                contentModel.AtCCV_DiscussionTopic_One = atCCVItem.AttributeValues["DiscussionTopic1"].ToString();
                contentModel.AtCCV_DiscussionTopic_Two = atCCVItem.AttributeValues["DiscussionTopic2"].ToString();

                string dateTime = atCCVItem.AttributeValues["WeekendDate"].ToString();
                if ( string.IsNullOrWhiteSpace( dateTime ) == false )
                {
                    contentModel.AtCCV_Date = DateTime.Parse( dateTime );
                }

                string seriesImageGuid = atCCVItem.AttributeValues["SeriesImage"].Value.ToString();
                if ( string.IsNullOrWhiteSpace( seriesImageGuid ) == false )
                {
                    contentModel.AtCCV_ImageURL = publicAppRoot + "GetImage.ashx?Guid=" + seriesImageGuid;
                }
                else
                {
                    contentModel.AtCCV_ImageURL = string.Empty;
                }

                // Faith building
                contentModel.FaithBuilding_Title = faithBuildingItem.AttributeValues["FBTitle"].ToString();
                contentModel.FaithBuilding_Content = faithBuildingItem.AttributeValues["FBContent"].ToString();

                // resources CAN be empty, so just take whatever's available
                contentModel.Resources = new List<KidsResourceModel>();

                foreach ( var resourceItem in resourceList )
                {
                    KidsResourceModel resModel = new KidsResourceModel
                    {
                        Title = resourceItem.AttributeValues["ResourceTitle"].ToString(),
                        Subtitle = resourceItem.AttributeValues["Subtitle"].ToString(),
                        URL = resourceItem.AttributeValues["URL"].ToString()
                    };

                    contentModel.Resources.Add( resModel );
                }

                return contentModel;
            }
            else
            {
                return null;
            }
        }

        public static List<LifeTrainingTopicModel> BuildLifeTrainingContent( )
        {
            RockContext rockContext = new RockContext();
            ContentChannelService contentChannelService = new ContentChannelService( rockContext );

            // first, get the Life Training Topics
            const int ContentChannelId_LifeTrainingTopics = 295;
            ContentChannel lifeTrainingTopics  = contentChannelService.Get( ContentChannelId_LifeTrainingTopics );

            // sort by priority
            var ltTopicItems = lifeTrainingTopics.Items.OrderByDescending( i => i.Priority);


            // next, get the Life Training Resources
            const int ContentChannelId_LifeTrainingResources = 296;
            ContentChannel ltResources = contentChannelService.Get( ContentChannelId_LifeTrainingResources );

            // sort by priority
            var ltResourceItems = ltResources.Items.OrderByDescending( i => i.Priority );

            // now build the list of models we'll send down
            string publicAppRoot = GlobalAttributesCache.Value( "PublicApplicationRoot" ).EnsureTrailingForwardslash();

            List<LifeTrainingTopicModel> ltTopicModels = new List<LifeTrainingTopicModel>();
            foreach ( var ltTopic in ltTopicItems )
            {
                ltTopic.LoadAttributes();

                LifeTrainingTopicModel ltTopicModel = new LifeTrainingTopicModel();
                ltTopicModel.Title = ltTopic.Title;
                ltTopicModel.Content = ltTopic.Content;

                // try getting the image
                string ltItemImageGuid = ltTopic.AttributeValues["Image"].Value.ToString();
                if ( string.IsNullOrWhiteSpace( ltItemImageGuid ) == false )
                {
                    ltTopicModel.ImageURL = publicAppRoot + "GetImage.ashx?Guid=" + ltItemImageGuid;
                }
                else
                {
                    ltTopicModel.ImageURL = string.Empty;
                }

                // try getting the "Talk to Someone" URL
                string ltItemTalkURL = ltTopic.AttributeValues["TalktoSomeoneURL"].Value.ToString();
                if ( string.IsNullOrWhiteSpace( ltItemTalkURL ) == false )
                {
                    ltTopicModel.TalkToSomeoneURL = ltItemTalkURL;
                }
                else
                {
                    ltTopicModel.TalkToSomeoneURL = string.Empty;
                }

                // now add all associated resources
                ltTopicModel.Resources = new List<LifeTrainingResourceModel>();
                foreach ( var resource in ltResourceItems )
                {
                    resource.LoadAttributes();

                    // is this resource associated with this topic?
                    string associatedTopics = resource.AttributeValues["AssociatedLifeTrainingTopics"].Value.ToString();
                    if ( associatedTopics.Contains( ltTopic.Title ) )
                    {
                        // then add it
                        LifeTrainingResourceModel resourceModel = new LifeTrainingResourceModel();
                        resourceModel.Title = resource.Title;
                        resourceModel.Content = resource.Content;
                        resourceModel.Author = resource.AttributeValues["Author"].Value.ToString();
                        resourceModel.URL = resource.AttributeValues["URL"].Value.ToString();

                        // add a hint for the mobile app so it knows whether to show a book detail page or not.
                        // if there's content, and an author, it's a book.
                        if ( string.IsNullOrWhiteSpace( resourceModel.Content ) == false &&
                            string.IsNullOrWhiteSpace( resourceModel.Author ) == false )
                        {
                            resourceModel.IsBook = true;
                        }

                        // is there an image?
                        string resourceImageGuid = resource.AttributeValues["Image"].Value.ToString();
                        if ( string.IsNullOrWhiteSpace( resourceImageGuid ) == false )
                        {
                            resourceModel.ImageURL = publicAppRoot + "GetImage.ashx?Guid=" + resourceImageGuid;
                        }
                        else
                        {
                            resourceModel.ImageURL = string.Empty;
                        }

                        ltTopicModel.Resources.Add( resourceModel );
                    }
                }

                // sort resources with books on top
                ltTopicModel.Resources = ltTopicModel.Resources.OrderByDescending( a => a.IsBook ).ToList();

                ltTopicModels.Add( ltTopicModel );
            }

            return ltTopicModels;
        }
    }
}
