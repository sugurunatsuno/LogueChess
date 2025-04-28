using LogueChess.Runtime;
using RougeChess.Runtime;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace LogueChess
{
    public class InGameLifetimeScope : LifetimeScope
    {
        public enum DIMode
        {
            Debug,
            Scriptable,
            
        }
        
        [SerializeField] DIMode diMode;
        
        [SerializeField] BattleUIService uiService; // シーン上の UIManager
        
        [Header("SO")]
        [SerializeField] PartyServiceSO partyService_so;
        [SerializeField] EnemyServiceSO enemyService_so;
        
        protected override void Configure(IContainerBuilder builder)
        {
            // DIのモードによってDIの方法を変える
            switch (diMode)
            {
                case DIMode.Debug:
                    builder.Register<PartyService>(Lifetime.Singleton).As<IPartyService>();
                    builder.Register<EnemyService>(Lifetime.Singleton).As<IEnemyService>();
                    break;
                case DIMode.Scriptable:
                    if (partyService_so == null || enemyService_so == null)
                    {
                        Debug.LogError("SO is not set.");
                        return;
                    }
                    builder.RegisterInstance(partyService_so).As<IPartyService>();
                    builder.RegisterInstance(enemyService_so).As<IEnemyService>();
                    break;
            }
            
            builder.Register<BattleService>(Lifetime.Singleton).As<IBattleService>();
            // builder.Register<DebugUIService>(Lifetime.Singleton).As<IUIService>();
            
            // ここでUIのインジェクション
            builder.RegisterInstance(uiService).As<IUIService>();
            builder.RegisterComponentInHierarchy<BattleSampleRunner>();
        }
    }
}
