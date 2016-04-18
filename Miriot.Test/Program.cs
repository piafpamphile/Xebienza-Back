using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Miriot.Common.Request;
using Newtonsoft.Json;

namespace Miriot.Test
{
    internal class Program
    {
        private const string ServiceUrl = "http://selfalliance.azurewebsites.net/";
        //private const string ServiceUrl = "http://localhost:63891/";
        private static Dictionary<string, string> _names = new Dictionary<string, string>();
        private static Dictionary<string, string> _errors = new Dictionary<string, string>();
        private static Dictionary<string, string> _creationErrorResult = new Dictionary<string, string>();

        private static void Main(string[] args)
        {
            try
            {
                var path = @"C:\Users\jpamphile\Desktop\Speakers Devoxx";
                MassImport(path).Wait();
                Console.ReadLine();
                foreach (var speaker in _names)
                {
                    var result = Identify(speaker.Value).Result;
                    if (string.IsNullOrWhiteSpace(result))
                        _errors.Add(speaker.Key, speaker.Value);
                }

                using (var file = new StreamWriter(@"C:\Users\jpamphile\Desktop\errors1.txt"))
                {
                    foreach (var line in _errors)
                    {
                        file.WriteLine(line.Key);
                    }
                }

                Console.ReadLine();
            }
            catch (Exception e)
            {

            }
        }

        private static async Task MassImport(string path)
        {
            var fileEntries = Directory.GetFiles(path);
            foreach (string fileName in fileEntries)
            {
                var name = string.Join(" ",
                    fileName.Split('\\').Last().Split('.')[0].Split('_')
                        .Select(o => o.Substring(0, 1).ToUpper() + o.Substring(1).ToLower()));
                Console.WriteLine(name);
                _names.Add(name, fileName);
                await CreateAsync(name, fileName);
            }
        }

        private static async Task<bool> CreateAsync(string name, string filePath)
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(ServiceUrl);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var request = new UserRequest
                {
                    Name = name,
                    Image = File.ReadAllBytes(filePath)
                };

                var response = await client.PutAsync("api/user",
                    new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json"));

                if (!response.IsSuccessStatusCode)
                    _creationErrorResult.Add(name + " : " + filePath, response.ReasonPhrase);

                return response.IsSuccessStatusCode;
            }
        }

        private static async Task<string> Identify(string filePath)
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(ServiceUrl);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var userToRecognize = new IdentificationRequest
                {
                    Image = File.ReadAllBytes(filePath)
                };

                var response = await client.PostAsync("api/user",
                    new StringContent(JsonConvert.SerializeObject(userToRecognize), Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                {
                    var user = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"User identified : {user}");
                    return user;
                }

                Console.WriteLine("Error while identifying the user !");
                return null;
            }
        }
    }
}