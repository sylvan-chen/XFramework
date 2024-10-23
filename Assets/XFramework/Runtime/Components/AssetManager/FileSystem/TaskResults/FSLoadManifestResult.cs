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
            return new FSLoadManifestResult(TaskResultStatus.Failure, default, error);
        }

        public readonly TaskResultStatus Status;
        public readonly Manifest Manifest;
        public readonly string Error;

        public FSLoadManifestResult(TaskResultStatus status, Manifest manifest, string error)
        {
            Status = status;
            Manifest = manifest;
            Error = error;
        }
    }
}