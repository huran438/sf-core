using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace SFramework.Core.Runtime
{
    public abstract class SFContextRoot : MonoBehaviour
    {
        private UniTaskCompletionSource _initializationCompletionSource;
        public bool IsInitialized { get; private set; }

        protected virtual void Awake()
        {
            _initializationCompletionSource = new UniTaskCompletionSource();
            PreInit();
            Bind();
            SFContainer.Setup();
        }

        private async UniTaskVoid Start()
        {
            await SFContainer.InitServices(destroyCancellationToken);
            await Init(destroyCancellationToken);
            IsInitialized = true;
            _initializationCompletionSource.TrySetResult();
        }
        
        public UniTask WaitForInitialization()
        {
            if (IsInitialized)
                return UniTask.CompletedTask;
            
            return _initializationCompletionSource.Task;
        }
        
        public UniTask.Awaiter GetAwaiter() => WaitForInitialization().GetAwaiter();

        protected abstract void PreInit();
        protected abstract void Bind();
        protected abstract UniTask Init(CancellationToken cancellationToken);
    }
}