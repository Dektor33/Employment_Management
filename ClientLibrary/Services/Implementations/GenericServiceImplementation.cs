using BaseLibrary.Responses;
using ClientLibrary.Helpers;
using ClientLibrary.Services.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace ClientLibrary.Services.Implementations
{
    public class GenericServiceImplementation<T>(GetHttpClient getHttpClient) : IGenericServiceInterface<T>
    {
        //Delete {id}
        public async Task<GeneralResponse> DeleteById(int id, string baseURL)
        {
            var httpClient = await getHttpClient.GetPrivateHttpClient();
            var response = await httpClient.DeleteAsync($"{baseURL}/delete/{id}");
            var result = await response.Content.ReadFromJsonAsync<GeneralResponse>();
            return result!;
        }

        //Read All
        public async Task<List<T>> GetAll(string baseUrl)
        {
            var httpClient = await getHttpClient.GetPrivateHttpClient();
            var result = await httpClient.GetFromJsonAsync<List<T>>($"{baseUrl}/all");
            return result!;
        }

        //Read Single {id}
        public async Task<T> GetById(int id, string baseUrl)
        {
            var httpClient = await getHttpClient.GetPrivateHttpClient();
            var result = await httpClient.GetFromJsonAsync<T>($"{baseUrl}/single/{id}");
            return result!;
        }

        //Create
        public async Task<GeneralResponse> Insert(T item, string baseUrl)
        {
            var httpClient = await getHttpClient.GetPrivateHttpClient();
            var response = await httpClient.PostAsJsonAsync($"{baseUrl}/add", item);
            var result = await response.Content.ReadFromJsonAsync<GeneralResponse>();
            return result!;
        }

        //Update {model}
        public async Task<GeneralResponse> Update(T item, string baseUrl)
        {
            var httpClient = await getHttpClient.GetPrivateHttpClient();
            var response = await httpClient.PutAsJsonAsync($"{baseUrl}/update", item);
            var result = await response.Content.ReadFromJsonAsync< GeneralResponse>();
            return result!;
        }
    }
}
