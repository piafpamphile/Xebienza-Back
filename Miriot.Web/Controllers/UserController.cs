using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;
using Miriot.Common.Model;
using Miriot.Common.Request;
using Miriot.Web.Tools;

namespace Miriot.Web.Controllers
{
    public class UserController : ApiController
    {
        private const string OxfordFaceKey = "76cad5e7644346669f094fa0315de735";
        private const string XebienzaPersonGroup = "usrxebienza";
        private readonly Storage _storage = new Storage();

        public UserController()
        {
            Task.Run(() => _storage.LogAsunc("Identification request received", Level.Info)).Wait();
            var faceClient = new FaceServiceClient(OxfordFaceKey);
            var groups = Task.Run(() => faceClient.GetPersonGroupsAsync()).Result;

            var xebienzaPersonGroup = groups.SingleOrDefault(o => o.Name == XebienzaPersonGroup);
            if (xebienzaPersonGroup == null)
            {
                Task.Run(() => faceClient.CreatePersonGroupAsync(XebienzaPersonGroup, XebienzaPersonGroup)).Wait();
                Task.Run(() => faceClient.TrainPersonGroupAsync(XebienzaPersonGroup)).Wait();
            }
        }

        [HttpGet]
        public IEnumerable<User> Get()
        {
            return _storage.GetUsersFromTableStorage();
        }

        [HttpGet]
        public async Task<User> Get(Guid id)
        {
            return await _storage.GetUserFromTableStorageAsync(id);
        }

        [HttpPost]
        [Route("api/user/reset/{id:Guid}", Name = "Reset")]
        public async Task<IHttpActionResult> Reset(Guid id)
        {
            try
            {
                var faceClient = new FaceServiceClient(OxfordFaceKey);

                var person = await faceClient.GetPersonAsync(XebienzaPersonGroup, id);

                foreach (var faceId in person.PersistedFaceIds)
                    await faceClient.DeletePersonFaceAsync(XebienzaPersonGroup, person.PersonId, faceId);

                return Ok();
            }
            catch (Exception e)
            {
                await _storage.LogAsunc("Error while processing message", Level.Error, e);
                return InternalServerError(e);
            }
        }

        [HttpPost]
        public async Task<IHttpActionResult> Post([FromBody]IdentificationRequest request)
        {
            try
            {
                await _storage.LogAsunc("Identification request received", Level.Info);

                if (request.Image == null)
                    return BadRequest();

                var faceClient = new FaceServiceClient(OxfordFaceKey);
                List<Face> faces;

                // Détection des visages sur la photo
                using (var stream = new MemoryStream(request.Image))
                    faces = (await faceClient.DetectAsync(stream)).ToList();

                await _storage.LogAsunc($"{faces.Count} faces detected", Level.Info);
                if (faces.Count == 0)
                    return NotFound();

                var result = (await faceClient.IdentifyAsync(XebienzaPersonGroup,
                    faces.Select(o => o.FaceId).ToArray())).ToList();

                await _storage.LogAsunc($"{result.Count} persons identified", Level.Info);
                if (result.Count == 0 || !result.Any(o => o.Candidates.Any()))
                    return NotFound();

                // Une fois les personnes identifiées, on ne garde que la mieux reconnue
                var moreConfidentPerson = result.SelectMany(p => p.Candidates)
                    .OrderByDescending(o => o.Confidence).First();

                // On récupère les informations de la personne avec ses préférences
                await _storage.LogAsunc($"{moreConfidentPerson.PersonId} is the more confident person", Level.Info);
                var user = await _storage.GetUserFromTableStorageAsync(moreConfidentPerson.PersonId);

                await _storage.LogAsunc($"Result: {user.Name}", Level.Info);

                // On enrichit la base de connaissance d'Oxford
                await _storage.LogAsunc("Training requested for identified persons", Level.Info);
                foreach (var person in result.Where(o => o.Candidates.Any()))
                    await _storage.AddPersonFaceAsync(person.Candidates
                        .OrderByDescending(o => o.Confidence).First().PersonId, request.Image);

                return Ok(user.Name);
            }
            catch (Exception e)
            {
                await _storage.LogAsunc("Error while processing message", Level.Error, e);
                throw;
            }
        }

        public async Task<IHttpActionResult> Put([FromBody]UserRequest request)
        {
            try
            {
                await _storage.LogAsunc("Create/Update request received", Level.Info);

                var faceClient = new FaceServiceClient(OxfordFaceKey);

                List<Face> faces;
                using (var stream = new MemoryStream(request.Image))
                    faces = (await faceClient.DetectAsync(stream)).ToList();

                var faceIds = faces.Select(o => o.FaceId).ToList();

                if (faceIds.Count != 1)
                    return BadRequest();

                var result = await faceClient.CreatePersonAsync(XebienzaPersonGroup, request.Name);

                if (result == null)
                    return InternalServerError();

                var personId = result.PersonId;

                if (request.Image != null)
                    await _storage.AddPersonFaceAsync(personId, request.Image);

                await _storage.CreateOrUpdateUserInTableStorageAsync(personId, request.Name, request.Widgets);

                return Ok();
            }
            catch (Exception e)
            {
                await _storage.LogAsunc("Error while processing message", Level.Error, e);
                return InternalServerError(e);
            }
        }

        public void Delete(Guid id)
        {
        }
    }
}
