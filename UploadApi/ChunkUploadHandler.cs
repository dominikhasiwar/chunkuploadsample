using System;
using System.Globalization;
using System.IO;
using System.Net.Http;

namespace UploadApi
{
    public class ChunkUploadHandler
    {
        private readonly string _uploadDir;

        public ChunkUploadHandler(string uploaDir)
        {
            _uploadDir = uploaDir;

            if (!Directory.Exists(_uploadDir))
                Directory.CreateDirectory(_uploadDir);
        }

        public bool IsChunkHere(int chunkNumber, string identifier)
        {
            var fileName = GetChunkFileName(chunkNumber, identifier);
            return File.Exists(fileName);
        }

        public bool TryAssembleFile(string identifier, int totalChunks, string filename)
        {
            if (!AreAllChunksHere(identifier, totalChunks))
                return false;

            // create a single file
            var consolidatedFileName = GetFileName(identifier);
            using (var destStream = File.Create(consolidatedFileName, 15000))
            {
                for (var chunkNumber = 1; chunkNumber <= totalChunks; chunkNumber++)
                {
                    string chunkFileName = GetChunkFileName(chunkNumber, identifier);
                    using (var sourceStream = File.OpenRead(chunkFileName))
                    {
                        sourceStream.CopyTo(destStream);
                    }
                }
                destStream.Close();
            }

            // rename consolidated with original name of upload
            // strip to filename if directory is specified (avoid cross-directory attack)
            filename = Path.GetFileName(filename);

            var realFileName = Path.Combine(_uploadDir, filename);
            if (File.Exists(filename))
                File.Delete(realFileName);
            File.Move(consolidatedFileName, realFileName);

            // delete chunk files
            for (var chunkNumber = 1; chunkNumber <= totalChunks; chunkNumber++)
            {
                var chunkFileName = GetChunkFileName(chunkNumber, identifier);
                File.Delete(chunkFileName);
            }
            return true;
        }

        public void RenameChunk(MultipartFileData chunk, int chunkNumber, string identifier)
        {
            var generatedFileName = chunk.LocalFileName;
            var chunkFileName = GetChunkFileName(chunkNumber, identifier);
            if (File.Exists(chunkFileName))
                File.Delete(chunkFileName);
            File.Move(generatedFileName, chunkFileName);
        }

        private bool AreAllChunksHere(string identifier, int totalChunks)
        {
            for (int nChunkNumber = 1; nChunkNumber <= totalChunks; nChunkNumber++)
                if (!IsChunkHere(nChunkNumber, identifier))
                    return false;
            return true;
        }

        private string GetFileName(string identifier)
        {
            return Path.Combine(_uploadDir, identifier);
        }

        private string GetChunkFileName(int chunkNumber, string identifier)
        {
            return Path.Combine(_uploadDir, String.Format(CultureInfo.InvariantCulture, "{0}_{1}", identifier, chunkNumber) + ".part");
        }
    }
}