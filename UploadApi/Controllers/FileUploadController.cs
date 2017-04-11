using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

namespace UploadApi.Controllers
{
    public class FileUploadController : ApiController
    {
        private const string UPLOAD_DIR = @"C:\Temp\Storage\Upload\";

        private readonly ChunkUploadHandler _uploadHandler = new ChunkUploadHandler(UPLOAD_DIR);        

        [HttpGet]
        public IHttpActionResult ChunkExists([FromUri] int flowChunkNumber, [FromUri] string flowIdentifier)
        {
            if (_uploadHandler.IsChunkHere(flowChunkNumber, flowIdentifier))
                return Ok();
            else
                return ResponseMessage(new HttpResponseMessage(HttpStatusCode.NoContent));
        }

        [HttpPost]
        public async Task<IHttpActionResult> Upload()
        {
            // ensure that the request contains multipart/form-data
            if (!Request.Content.IsMimeMultipartContent())
                throw new HttpResponseException(HttpStatusCode.UnsupportedMediaType);

            var provider = new MultipartFormDataStreamProvider(UPLOAD_DIR);
            try
            {
                await Request.Content.ReadAsMultipartAsync(provider);

                var chunkNumber = Convert.ToInt32(provider.FormData["flowChunkNumber"]);
                var totalChunks = Convert.ToInt32(provider.FormData["flowTotalChunks"]);
                var identitifier = provider.FormData["flowIdentifier"];
                var fileName = provider.FormData["flowFilename"];

                // rename the generated file
                var chunk = provider.FileData[0];
                _uploadHandler.RenameChunk(chunk, chunkNumber, identitifier);

                // assemble chunks into single file if they're all here
                _uploadHandler.TryAssembleFile(identitifier, totalChunks, fileName);

                return Ok();
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }     
    }
}