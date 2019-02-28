﻿using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using church.ccv.Actions;
using church.ccv.CCVRest.MobileApp.Model;
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;

namespace church.ccv.CCVRest.MobileApp.Util
{
    public class Util
    {
        public static PersonModel GetPersonModel( int personId )
        {
            RockContext rockContext = new RockContext();
            PersonService personService = new PersonService( rockContext );
            GroupService groupService = new GroupService( rockContext );
            UserLoginService userLoginService = new UserLoginService( rockContext );


            // start by getting the person. if we can't do that, we should fail
            Person person = personService.Queryable().Include( a => a.PhoneNumbers ).Include( a => a.Aliases )
                .FirstOrDefault( p => p.Id == personId );

            if ( person == null )
            {
                return null;
            }

            PersonModel personModel = new PersonModel();

            // first get their basic info
            personModel.PrimaryAliasId = person.PrimaryAliasId ?? person.Id;
            personModel.FirstName = person.NickName;
            personModel.LastName = person.LastName;
            personModel.Email = person.Email;
            personModel.PhotoId = person.PhotoId;
            personModel.Birthdate = person.BirthDate;

            var mobilePhoneType = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_MOBILE.AsGuid() );
            if ( mobilePhoneType != null )
            {
                PhoneNumber phoneNumber = person.PhoneNumbers.FirstOrDefault( n => n.NumberTypeValueId == mobilePhoneType.Id );
                if ( phoneNumber != null )
                {
                    personModel.PhoneNumberDigits = phoneNumber.Number;
                }
            }

            // now get info about their family
            Group family = person.GetFamily();
            personModel.FamilyId = family.Id;

            personModel.FamilyMembers = new List<PersonModel.FamilyMember>();
            foreach ( GroupMember groupMember in family.Members )
            {
                if ( groupMember.Person.Id != personId )
                {
                    PersonModel.FamilyMember familyMember = new PersonModel.FamilyMember
                    {
                        PrimaryAliasId = groupMember.Person.PrimaryAliasId ?? groupMember.Person.Id,
                        FirstName = groupMember.Person.FirstName,
                        LastName = groupMember.Person.LastName,
                        PhotoId = groupMember.Person.PhotoId

                    };

                    personModel.FamilyMembers.Add( familyMember );
                }
            }

            // now try to get ther home address for this family
            Guid? homeAddressGuid = Rock.SystemGuid.DefinedValue.GROUP_LOCATION_TYPE_HOME.AsGuidOrNull();
            if ( homeAddressGuid.HasValue )
            {
                var homeAddressDv = DefinedValueCache.Read( homeAddressGuid.Value );
                if ( homeAddressDv != null )
                {
                    // take the group location flagged as a home address and mapped
                    GroupLocation familyAddress = family.GroupLocations
                        .Where( l =>
                            l.GroupLocationTypeValueId == homeAddressDv.Id &&
                            l.IsMappedLocation )
                        .FirstOrDefault();

                    if ( familyAddress != null )
                    {
                        personModel.Street1 = familyAddress.Location.Street1;
                        personModel.Street2 = familyAddress.Location.Street2;
                        personModel.City = familyAddress.Location.City;
                        personModel.State = familyAddress.Location.State;
                        personModel.Zip = familyAddress.Location.PostalCode;
                    }
                }
            }

            // their age determines whether we use adult vs student actions. If they have no age, or are >= 18, they're an adult
            if ( person.Age.HasValue == false || person.Age >= 18 )
            {
                DateTime? baptismDate;
                personModel.IsBaptised = Actions_Adult.Baptised.IsBaptised( person.Id, out baptismDate );
                personModel.IsERA = Actions_Adult.ERA.IsERA( person.Id );
                personModel.IsGiving = Actions_Adult.Give.IsGiving( person.Id );

                DateTime? membershipDate;
                personModel.IsMember = Actions_Adult.Member.IsMember( person.Id, out membershipDate );

                Actions_Adult.Mentored.Result mentoredResult;
                Actions_Adult.Mentored.IsMentored( person.Id, out mentoredResult );
                personModel.IsMentored = mentoredResult.IsMentored;

                Actions_Adult.PeerLearning.Result peerLearningResult;
                Actions_Adult.PeerLearning.IsPeerLearning( person.Id, out peerLearningResult );
                personModel.IsPeerLearning = peerLearningResult.IsPeerLearning;

                Actions_Adult.Serving.Result servingResult;
                Actions_Adult.Serving.IsServing( person.Id, out servingResult );
                personModel.IsServing = servingResult.IsServing;

                Actions_Adult.Teaching.Result teachingResult;
                Actions_Adult.Teaching.IsTeaching( person.Id, out teachingResult );
                personModel.IsTeaching = teachingResult.IsTeaching;

                DateTime? startingPointDate;
                personModel.TakenStartingPoint = Actions_Adult.StartingPoint.TakenStartingPoint( person.Id, out startingPointDate );

                List<int> storyIds;
                personModel.SharedStory = Actions_Adult.ShareStory.SharedStory( person.Id, out storyIds );
            }
            // get the students version
            else
            {
                DateTime? baptismDate;
                personModel.IsBaptised = Actions_Student.Baptised.IsBaptised( person.Id, out baptismDate );
                personModel.IsERA = Actions_Student.ERA.IsERA( person.Id );
                personModel.IsGiving = Actions_Student.Give.IsGiving( person.Id );

                DateTime? membershipDate;
                personModel.IsMember = Actions_Student.Member.IsMember( person.Id, out membershipDate );

                Actions_Student.Mentored.Result mentoredResult;
                Actions_Student.Mentored.IsMentored( person.Id, out mentoredResult );
                personModel.IsMentored = mentoredResult.IsMentored;

                Actions_Student.PeerLearning.Result peerLearningResult;
                Actions_Student.PeerLearning.IsPeerLearning( person.Id, out peerLearningResult );
                personModel.IsPeerLearning = peerLearningResult.IsPeerLearning;

                Actions_Student.Serving.Result servingResult;
                Actions_Student.Serving.IsServing( person.Id, out servingResult );
                personModel.IsServing = servingResult.IsServing;

                Actions_Student.Teaching.Result teachingResult;
                Actions_Student.Teaching.IsTeaching( person.Id, out teachingResult );
                personModel.IsTeaching = teachingResult.IsTeaching;

                DateTime? startingPointDate;
                personModel.TakenStartingPoint = Actions_Student.StartingPoint.TakenStartingPoint( person.Id, out startingPointDate );

                List<int> storyIds;
                personModel.SharedStory = Actions_Student.ShareStory.SharedStory( person.Id, out storyIds );
            }

            return personModel;
        }
    }
}
