using System;

namespace XFramework.Resource
{
    public readonly struct FSLoadManifestResult
    {
        public static FSLoadManifestResult Success(Manifest manifest)
        {
            return new FSLoadManifestResult(TaskResultStatus.Success, manifest, null);
        }

        public static FSLoadManifestResult Failure(string error)
        {
            return new FSLoadManifestResult(TaskResultStatus.Failure, new Manifest(), error);
        }

        private readonly TaskResultStatus _status;
        private readonly Manifest _manifest;
        private readonly string _error;

        public FSLoadManifestResult(TaskResultStatus status, Manifest manifestInfo, string error)
        {
            _status = status;
            _manifest = manifestInfo;
            _error = error;
        }

        public TaskResultStatus Status
        {
            get => _status;
        }

        public string Error
        {
            get => _error;
        }

        public Manifest Manifest
        {
            get
            {
                if (Status == TaskResultStatus.Failure)
                {
                    throw new InvalidOperationException($"Cannot access ManifestInfo of {nameof(FSLoadManifestResult)} when the status is Failure.");
                }
                return _manifest;
            }
        }
    }
}