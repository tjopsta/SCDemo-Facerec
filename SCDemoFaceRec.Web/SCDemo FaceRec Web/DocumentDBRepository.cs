using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using SCDemoFaceRecWeb.Models;
using Newtonsoft.Json;

namespace SCDemoFaceRec.Web
{
    public interface IDocumentDBRepository
    {
        Task<IEnumerable<Person>> GetItemsFromCollectionAsync();
        Task<Person> GetItemFromCollectionAsync(string id);
    }

    public class DocumentDBRepository : IDocumentDBRepository
    {
        #region The DocumentDB Endpoint, Key, DatabaseId and CollectionId declaration
        private static readonly string Endpoint = "https://scdemo-face-events-db.documents.azure.com:443/";
        private static readonly string Key = "PcFT1azvEPA1B6AORbbzao6X6hnieqCyC7IButN43ymQgzyOCZgzBeDlKyRydmpPWAIjpC1J8bQ77KaQwlSXqg==";
        private static readonly string DatabaseId = "FaceDetections";
        private static readonly string CollectionId = "Faces";
        private static DocumentClient client;
        #endregion

        public DocumentDBRepository()
        {
            client = new DocumentClient(new Uri(Endpoint), Key);
            CreateDatabaseIfNotExistsAsync().Wait();
            CreateCollectionIfNotExistsAsync().Wait();
        }
        #region Private methods to create Database and Collection if not Exist
        /// <summary>
        /// The following function has following steps
        /// 1. Try to read database based on the DatabaseId passed as URI link, if it is not found the exception will be thrown
        /// 2. In the exception, the database will be created of which Id will be set as DatabaseId 
        /// </summary>
        /// <returns></returns>
        private static async Task CreateDatabaseIfNotExistsAsync()
        {
            try
            {
                //1.
                await client.ReadDatabaseAsync(UriFactory.CreateDatabaseUri(DatabaseId));
            }
            catch (DocumentClientException e)
            {
                if (e.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    //2.
                    await client.CreateDatabaseAsync(new Database { Id = DatabaseId });
                }
                else
                {
                    throw;
                }
            }
        }
        /// <summary>
        /// The following function has following steps
        /// 1.Read the collection based on the DatabaseId and Collectionid passed as URI, if not found then throw exception
        /// //2.In exception create a collection.
        /// </summary>
        /// <returns></returns>
        private static async Task CreateCollectionIfNotExistsAsync()
        {
            try
            {
                //1.
                await client.ReadDocumentCollectionAsync(UriFactory.CreateDocumentCollectionUri(DatabaseId, CollectionId));
            }
            catch (DocumentClientException e)
            {
                if (e.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    //2.
                    await client.CreateDocumentCollectionAsync(
                        UriFactory.CreateDatabaseUri(DatabaseId),
                        new DocumentCollection { Id = CollectionId },
                        new RequestOptions { OfferThroughput = 1000 });
                }
                else
                {
                    throw;
                }
            }
        }
        #endregion

        /// <summary>
        /// Method to Read all Documents from the collection
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<Person>> GetItemsFromCollectionAsync()
        {
            var documents = client.CreateDocumentQuery<Person>(
                  UriFactory.CreateDocumentCollectionUri(DatabaseId, CollectionId),
                  new FeedOptions { MaxItemCount = -1 })
                  .AsDocumentQuery();
            List<Person> persons = new List<Person>();
            while (documents.HasMoreResults)
            {
                persons.AddRange(await documents.ExecuteNextAsync<Person>());
            }
            return persons;
        }

        /// <summary>
        /// Method to read Item from the document based on id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<Person> GetItemFromCollectionAsync(string id)
        {
            try
            {
                Document doc = await client.ReadDocumentAsync(UriFactory.CreateDocumentUri(DatabaseId, CollectionId, id));
                return JsonConvert.DeserializeObject<Person>(doc.ToString());
            }
            catch (DocumentClientException e)
            {
                if (e.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return null;
                }
                else
                {
                    throw;
                }
            }
        }
    }
}
