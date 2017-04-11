using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using Microsoft.Win32;

namespace UploadApiWPFClient
{
    public class MainViewModel : ViewModelBase
    {
        private string _fileName;
        public string FileName
        {
            get { return _fileName; }
            set { Set(ref _fileName, value); }
        }

        private bool _uploading;
        public bool Uploading
        {
            get { return _uploading; }
            set { Set(ref _uploading, value); }
        }

        private int _progress;
        public int Progress
        {
            get { return _progress; }
            set { Set(ref _progress, value); }
        }

        private RelayCommand _selectFileCommand;
        public RelayCommand SelectFileCommand
        {
            get { return _selectFileCommand ?? (_selectFileCommand = new RelayCommand(SelectFile, CanSelectFile)); }
        }
        private bool CanSelectFile() { return true; }
        private void SelectFile()
        {
            var dlg = new OpenFileDialog();
            dlg.Multiselect = false;

            if (dlg.ShowDialog() == true)
            {
                FileName = dlg.FileName;
            }
        }

        private RelayCommand _uploadCommand;
        public RelayCommand UploadCommand
        {
            get { return _uploadCommand ?? (_uploadCommand = new RelayCommand(async () => await Upload(), CanUpload)); }
        }
        private bool CanUpload() { return !Uploading && !string.IsNullOrEmpty(FileName); }
        private async Task Upload()
        {
            if (CanUpload())
            {
                Uploading = true;

                await StartChunkUpload(FileName, new Progress<int>(OnProgressChanged));

                Uploading = false;
            }
        }

        private void OnProgressChanged(int progress)
        {
            if (progress != Progress)
                Progress = progress;
        }

        public async Task StartChunkUpload(string fileName, IProgress<int> progress)
        {
            var chunkSize = 50 * 1024 * 1024;
            var uploadUri = "http://localhost:59509/api/fileupload";
            var identifier = Guid.NewGuid().GetHashCode().ToString("x08");
            using (var fileStream = File.OpenRead(fileName))
            using (var client = new HttpClient())
            {
                var count = 1;
                var totalChunkCount = Math.Round((double)fileStream.Length / chunkSize);
                progress.Report(0);
                var bytesRead = 0;
                while (fileStream.Position < fileStream.Length)
                {
                    var readCount = fileStream.Length - fileStream.Position > chunkSize ? chunkSize : (int)(fileStream.Length - fileStream.Position);
                    var buffer = new byte[readCount];
                    bytesRead = fileStream.Read(buffer, 0, readCount);
                    using (var chunkedStream = new MemoryStream(buffer))
                    using (var content = new MultipartFormDataContent())
                    {
                        content.Add(new StreamContent(chunkedStream), Path.GetFileNameWithoutExtension(fileName), Path.GetFileName(fileName));
                        content.Add(new StringContent(count.ToString()), "flowChunkNumber");
                        content.Add(new StringContent(totalChunkCount.ToString()), "flowTotalChunks");
                        content.Add(new StringContent(identifier), "flowIdentifier");
                        content.Add(new StringContent(Path.GetFileName(fileName)), "flowFilename");

                        await client.PostAsync(uploadUri, content);
                    }
                    count++;
                    progress.Report((int)(100 / totalChunkCount * count));
                }
            }
            progress.Report(100);
        }
    }
}
