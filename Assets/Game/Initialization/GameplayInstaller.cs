using System;
using Game.Analytics;
using Game.Analytics.Data;
using Game.AssetManagement;
using Game.Boosters;
using Game.Camera;
using Game.Currency;
using Game.Effects;
using Game.Gameplay;
using Game.Gameplay.Chapters;
using Game.Levels;
using Game.Profile;
using Game.Projectiles;
using Game.UI.Hud;
using Game.UI.Notifications;
using Game.UI.Windows.General;
using Game.UI.Windows.TimeLoseWindow;
using Game.UI.Windows.MenuWindow;
using Game.UI.Windows.PauseWindow;
using Game.UI.Windows.CheatsWindow;
using Game.UI.Windows.WinWindow;
using Game.UI.Windows.PreloadWindow;
using Game.UI.Windows.DownloadWaitWindow;
using Game.Utils;
using Game.Purchase;
using Game.SFX;
using Game.UI.Windows.BoosterWindow;
using Game.UI.Windows.BoosterUnlockWindow;
using Game.Ads;
using UnityEngine;
using Zenject;
using Signals = Game.Profile.Signals;

namespace Game.Initialization
{
    public class GameplayInstaller : MonoInstaller
    {
        [SerializeField] private WindowSystem _windowSystem;
        [SerializeField] private HudView _hudView;
        [SerializeField] private FreezeTimerBoosterData freezeTimerBoosterData;
        [SerializeField] private CompassBoosterData compassBoosterData;
        [SerializeField] private EffectsData effectsData;
        [SerializeField] private Projectile _projectilePrefab;
        [SerializeField] private SfxData _sfxData;
        [SerializeField] private SfxManager _sfxManager;
        [SerializeField] private MusicManager _musicManager;
        [SerializeField] private AnalyticsData _analyticsData;
        [SerializeField] private UnityLifecycleSystem _unityLifecycleSystem;
        [SerializeField] private GoldCurrencyData _goldCurrencyData;
        
        
        [SerializeField] private LevelController _levelControllerPrefab;
        
        public override void InstallBindings()
        {
            InstallSignalBus();
            InstallAnalytics();
            InstallStartup();
            InstallCamera();
            InstallEffects();
            InstallGameplay();
            InstallAds();
            InstallWindows();
            InstallHud();
            InstallUtils();
            InstallProfile();
            InstallProjectiles();
            InstallAudio();
        }

        private void InstallAudio()
        {
            Container.BindInterfacesAndSelfTo<AudioSystem>()
                .AsSingle();

            Container.BindInterfacesAndSelfTo<ConsumeSfxController>()
                .AsSingle();

            Container.BindInterfacesAndSelfTo<SfxManager>()
                .FromComponentInNewPrefab(_sfxManager)
                .AsSingle();

            Container.BindInstance(_sfxData)
                .AsSingle();
            
            Container.BindInterfacesAndSelfTo<MusicManager>()
                .FromComponentInNewPrefab(_musicManager)
                .AsSingle();
        }

        private void InstallProjectiles()
        {
            Container.BindInterfacesAndSelfTo<ProjectileSystem>()
                .AsSingle();

            Container.BindMemoryPool<Projectile, Projectile.Pool>()
                .WithInitialSize(5)
                .FromComponentInNewPrefab(_projectilePrefab)
                .UnderTransformGroup("ProjectilesPool");
        }

        private void InstallEffects()
        {
            Container.BindInterfacesAndSelfTo<EffectSystem>()
                .AsSingle()
                .WithArguments(effectsData);
        }

        private void InstallCamera()
        {
            Container.Bind<MainCameraController>()
                .FromComponentInHierarchy()
                .AsSingle();
        }

        private void InstallStartup()
        {
            Container.BindInterfacesAndSelfTo<GameStartupProcessor>()
                .AsSingle();

            Container.BindInterfacesAndSelfTo<UnityLifecycleSystem>()
                .FromComponentInNewPrefab(_unityLifecycleSystem)
                .AsSingle();
        }

        /// <summary>Installs gameplay together with disposable level-content reserve and loaded-level owners.</summary>
        private void InstallGameplay()
        {
            Container.BindInterfacesAndSelfTo<ChapterSystem>().AsSingle();
            Container.BindInterfacesAndSelfTo<GameplaySystem>()
                .AsSingle();

            Container.BindInterfacesAndSelfTo<PauseService>()
                .AsSingle();
            
            Container.Bind(typeof(LevelsManager), typeof(IPausable), typeof(IDisposable))
                .To<LevelsManager>()
                .AsSingle();

            Container.BindInterfacesAndSelfTo<LevelContentSystem>()
                .AsSingle();

            Container.BindInterfacesAndSelfTo<GameplayProgressController>()
                .AsSingle();
            
            Container.BindInterfacesAndSelfTo<BoostersProfileController>()
                .AsSingle();
            
            Container.Bind<ProfileHolder>()
                .To<GameplayProgressController>()
                .FromResolve();
            
            Container.Bind<ProfileHolder>()
                .To<BoostersProfileController>()
                .FromResolve();

            Container.BindInterfacesAndSelfTo<BoosterSystem>()
                .AsSingle();

            Container.Bind<Booster>()
                .To<CompassBooster>()
                .AsSingle()
                .WithArguments(compassBoosterData);

            Container.Bind<Booster>()
                .To<FreezeTimerBooster>()
                .AsSingle()
                .WithArguments(freezeTimerBoosterData);
            
            Container.BindInterfacesAndSelfTo<LevelProgressController>()
                .AsSingle();

            Container.BindInterfacesAndSelfTo<LevelController>()
                .FromComponentInNewPrefab(_levelControllerPrefab)
                .AsSingle();

            InstallCurrency();
            InstallPurchase();
        }

        private void InstallPurchase()
        {
            Container.BindInterfacesAndSelfTo<RewardSystem>()
                .AsSingle();

            Container.Bind<IRewardProcessor>()
                .To<CurrencyRewardProcessor>()
                .AsSingle();

            Container.Bind<IRewardProcessor>()
                .To<BoosterRewardProcessor>()
                .AsSingle();

            Container.BindInterfacesAndSelfTo<PurchaseSystem>()
                .AsSingle();

            Container.Bind<IPurchaseProcessor>()
                .To<CurrencyPriceProcessor>()
                .AsSingle();
        }

        /// <summary>Registers persisted gold behind the shared reward and counter API.</summary>
        private void InstallCurrency()
        {
            Container.BindInterfacesAndSelfTo<GoldCurrencyController>()
                .AsSingle()
                .WithArguments(_goldCurrencyData);

            Container.Bind<ProfileHolder>()
                .To<GoldCurrencyController>()
                .FromResolve();

            Container.BindInterfacesAndSelfTo<CurrencySystem>()
                .AsSingle();
        }

        private void InstallAds()
        {
            Container.BindInterfacesAndSelfTo<LionAdsProvider>()
                .AsSingle();

            Container.Bind<AdsProvider>()
                .To<LionAdsProvider>()
                .FromResolve();

            Container.BindInterfacesAndSelfTo<AdsSystem>()
                .AsSingle();
        }
        
        /// <summary>Registers the existing window infrastructure and the passive download-retry model processor.</summary>
        private void InstallWindows()
        {
            Container.Bind<IWindowModelProcessor>().To<ChapterWindowModelProcessor<ChapterInvestigationWindowModel>>().AsSingle();
            Container.Bind<IWindowModelProcessor>().To<ChapterWindowModelProcessor<ChapterResultWindowModel>>().AsSingle();
            Container.Bind<IWindowModelProcessor>()
                .To<DownloadWaitWindowModelProcessor>()
                .AsSingle();
            Container.BindInterfacesAndSelfTo<WindowSystem>()
                .FromComponentInNewPrefab(_windowSystem)
                .AsSingle();

            Container.Bind<IWindowModelProcessor>()
                .To<PreloadWindowModelProcessor>()
                .AsSingle();

            Container.Bind<IWindowModelProcessor>()
                .To<MenuWindowModelProcessor>()
                .AsSingle();

            Container.Bind<IWindowModelProcessor>()
                .To<BoosterWindowModelProcessor>()
                .AsSingle();

            Container.Bind<IWindowModelProcessor>()
                .To<BoosterUnlockWindowModelProcessor>()
                .AsSingle();

            Container.Bind<IWindowModelProcessor>()
                .To<WinWindowModelProcessor>()
                .AsSingle();

            Container.Bind<IWindowModelProcessor>()
                .To<TimeLoseWindowModelProcessor>()
                .AsSingle();

            Container.Bind<IWindowModelProcessor>()
                .To<PauseWindowModelProcessor>()
                .AsSingle();

            Container.Bind<IWindowModelProcessor>()
                .To<CheatsWindowModelProcessor>()
                .AsSingle();
            
            Container.Bind<EscapeButtonWindowsHandler>()
                .FromComponentInHierarchy()
                .AsSingle();
        }

        /// <summary>Creates the HUD and wires its scene-owned notification queue for general UI feedback.</summary>
        private void InstallHud()
        {
            Container.BindInterfacesAndSelfTo<HudView>()
                .FromComponentInNewPrefab(_hudView)
                .AsSingle();

            Container.BindInterfacesAndSelfTo<HudModelProcessor>()
                .AsSingle();

            Container.Bind<TextNotificationView>()
                .FromMethod(context => context.Container.Resolve<HudView>().GetComponentInChildren<TextNotificationView>(true))
                .AsSingle();
            Container.BindInterfacesAndSelfTo<TextNotificationSystem>().AsSingle();

        }

        private void InstallUtils()
        {
            Container.BindInterfacesAndSelfTo<AddressablesAssetSystem>()
                .AsSingle();
            
            Container.Bind(typeof(Ticker), typeof(IPausable), typeof(ITickable))
                .To<Ticker>()
                .AsSingle();
        }

        private void InstallAnalytics()
        {
            Container.BindInterfacesAndSelfTo<AnalyticsInitializer>()
                .AsSingle()
                .WithArguments(_analyticsData)
                .NonLazy();
            
            Container.BindInterfacesAndSelfTo<LionSdkController>()
                .AsSingle();
        }

        private void InstallSignalBus()
        {
            SignalBusInstaller.Install(Container);
            
            Container.DeclareSignal<Signals.ProfileHolderAppeared>().OptionalSubscriber();
            
            Container.DeclareSignal<Currency.Signals.CurrencyAdded>().OptionalSubscriber();
            Container.DeclareSignal<Currency.Signals.CurrencySubtracted>().OptionalSubscriber();
            Container.DeclareSignal<Currency.Signals.NotEnoughCurrency>().OptionalSubscriber();
            
            Container.DeclareSignal<UI.Buttons.Signals.ButtonClickedSignal>().OptionalSubscriber();
            Container.DeclareSignal<Tutorial.Signals.ButtonClicked>().OptionalSubscriber();

            Container.DeclareSignal<Tutorial.Signals.TutorialCompletedSignal>().OptionalSubscriber();
            Container.DeclareSignal<Tutorial.Signals.TutorialStepCompletedSignal>().OptionalSubscriber();
            Container.DeclareSignal<Tutorial.Signals.ItemEatenSignal>().OptionalSubscriber();
            Container.DeclareSignal<Tutorial.Signals.ShowButtonRequested>().OptionalSubscriber();
            
            Container.DeclareSignal<Gameplay.Signals.LevelStarted>().OptionalSubscriber();
            Container.DeclareSignal<Gameplay.Signals.LevelFinished>().OptionalSubscriber();
            Container.DeclareSignal<Gameplay.Signals.LevelFailed>().OptionalSubscriber();
            Container.DeclareSignal<Gameplay.Signals.LevelFailedSoft>().OptionalSubscriber();
            Container.DeclareSignal<Gameplay.Signals.LevelAbandoned>().OptionalSubscriber();
            Container.DeclareSignal<Singals.PurchaseSucceed>().OptionalSubscriber();
            Container.DeclareSignal<Boosters.Signals.BoosterUsed>().OptionalSubscriber();
            Container.DeclareSignal<UI.Windows.Signals.ExtraTimeBoughtSignal>().OptionalSubscriber();
            Container.DeclareSignal<UI.Windows.Signals.ExtraLifeBoughtSignal>().OptionalSubscriber();
        }

        private void InstallProfile()
        {
            Container.BindInterfacesAndSelfTo<ProfileSystem>()
                .AsSingle();
        }
    }
}
