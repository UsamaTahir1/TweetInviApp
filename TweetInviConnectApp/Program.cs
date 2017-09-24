using LiteDB;
using System;
using System.Linq;
using Tweetinvi;
using Tweetinvi.Models;
using Tweetinvi.Parameters;

namespace TweetInviConnectApp
{
    class Program
    {
        private static string consumerKey = "9RIpNPfjGIvCYTErz18AvqRGc";
        private static string consumerSecret = "LisF5BwB2YVT5G3qDGQHDF3ePwTZ0mELA4Qd8RMppzjxyxEBZX";
        private static string userAccessKey = "911531991786491905-1bXB22IMPlblvhh7ZBcGYkbn0nkQCSj";
        private static string userAccessSecret = "sfe5mhoL5HNmjaNWpkPk8ilIBU8TYs9my7QqYavGCeuhR";
        private static string connectionString = "TweetInviData.db";
        private static int maxNumberOfResults = 10;
        static void Main(string[] args)
        {
            try
            {   
                using (var db = new LiteDatabase(connectionString))
                {
                    // Get a collection (or create, if doesn't exist)
                    var tweetsCollection = db.GetCollection<BsonDocument>("Tweets");
                    var followersCollection = db.GetCollection<BsonDocument>("Followers");

                    Console.WriteLine("Before:");
                    tweetsCollection.FindAll().ToList().ForEach(p => { Console.WriteLine(p.ToString()); });
                    followersCollection.FindAll().ToList().ForEach(p => { Console.WriteLine(p.ToString()); });

                    Auth.SetUserCredentials(consumerKey, consumerSecret, userAccessKey, userAccessSecret);

                    RateLimit.RateLimitTrackerMode = RateLimitTrackerMode.TrackOnly;
                    RateLimits_ManualAwait();
                    GetCurrentCredentialsRateLimits();

                    var user = User.GetAuthenticatedUser();
                    Console.WriteLine("Current User: "+ user);

                    FollowersHelper(followersCollection, user);
                    TweetSearchHelper(tweetsCollection);
                    
                    Console.WriteLine("After:");
                    tweetsCollection.FindAll().ToList().ForEach(p => { Console.WriteLine(p.ToString()); });
                    followersCollection.FindAll().ToList().ForEach(p => { Console.WriteLine(p.ToString()); });
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            GetCurrentCredentialsRateLimits();
            Console.Write("Press Any Key to Exit...");
            Console.ReadKey();
        }


        public static void FollowersHelper(LiteCollection<BsonDocument> followersCollection,IAuthenticatedUser user)
        {
            var followers = user.GetFollowers();
            Console.WriteLine("Total Followers: " + followers.Count());
            foreach (var follower in followers)
            {
                if (!followersCollection.Find(Query.EQ("FollowerId", follower.IdStr)).Any())
                {
                    var document = new BsonDocument
                            {
                                    { "FollowerId", follower.IdStr},
                                    { "FollowerName", follower.Name }
                            };
                    followersCollection.Insert(document);
                }
            }
        }
        public static void TweetSearchHelper(LiteCollection<BsonDocument> tweetsCollection)
        {
            Console.Write("Enter Hashtag to search Tweets: ");
            var hastag = Console.ReadLine();
            var searchParameter = new SearchTweetsParameters(hastag)
            {
                MaximumNumberOfResults = maxNumberOfResults,
                Filters = TweetSearchFilters.Hashtags
            };
            var matchingTweets = Search.SearchTweets(searchParameter);
            Console.WriteLine("Tweets against Hashtag" + matchingTweets.Count());
            foreach (var match in matchingTweets)
            {
                if (!tweetsCollection.Find(Query.EQ("TweetId", match.IdStr)).Any())
                {
                    var document = new BsonDocument
                            {
                              { "TweetId", match.IdStr},
                              { "Tweet", match.Text },
                              { "CreatedBy", match.CreatedBy.Name }
                            };
                    tweetsCollection.Insert(document);
                }
            }
        }
        public static void RateLimits_ManualAwait()
        {
            TweetinviEvents.QueryBeforeExecute += (sender, args) =>
            {
                var queryRateLimit = RateLimit.GetQueryRateLimit(args.QueryURL);
                RateLimit.AwaitForQueryRateLimit(queryRateLimit);
            };
        }
        public static void GetCurrentCredentialsRateLimits()
        {
            var tokenRateLimits = RateLimit.GetCurrentCredentialsRateLimits();

            Console.WriteLine("Remaning Requests for GetRate : {0}", tokenRateLimits.ApplicationRateLimitStatusLimit.Remaining);
            Console.WriteLine("Total Requests Allowed for GetRate : {0}", tokenRateLimits.ApplicationRateLimitStatusLimit.Limit);
            Console.WriteLine("GetRate limits will reset at : {0} local time", tokenRateLimits.ApplicationRateLimitStatusLimit.ResetDateTime.ToString("T"));
        }
    }
}
