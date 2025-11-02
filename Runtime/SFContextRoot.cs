using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace SFramework.Core.Runtime
{
    public abstract class SFContextRoot : MonoBehaviour
    {
        internal static SFContainer _Container => _container;
        protected static ISFContainer Container => _container;
        private static SFContainer _container;

        private UniTaskCompletionSource _initializationCompletionSource;
        public bool IsInitialized { get; private set; }

        protected virtual void Awake()
        {
            _initializationCompletionSource = new UniTaskCompletionSource();
            PreInit();
            _container = new SFContainer(gameObject);
            Bind(_container);
            _container.Inject();
        }

        private async UniTaskVoid Start()
        {
            await _container.InitServices(destroyCancellationToken);
            await Init(_container, destroyCancellationToken);
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
        protected abstract void Bind(SFContainer container);
        protected abstract UniTask Init(ISFContainer container, CancellationToken cancellationToken);
    }
}