using GlobalEnums;
using HutongGames.PlayMaker.Actions;
using System.Collections;
using Vasi;

namespace Kindred_Spirit
{
    internal class Boss : MonoBehaviour
    {

        public static  GameObject Vessel;
        private GameObject corpse;
        private bool died = false;
        private bool infury = false;
        private bool furyactivated = false;
        private ParticleSystem furyeffect;
        private GameObject furybursteffect;
        private AudioClip hurtsound;
        private GameObject headobject;
        private Vector3 vesselspawnpos = new Vector3(54.982f, 7.4081f);
        private Dictionary<string, PlayMakerFSM> SlashFsms = new Dictionary<string, PlayMakerFSM>();
        private Dictionary<string, string> SlashEffectAnims = new Dictionary<string, string>()
        {
            {"Slash","SlashEffect"},
            {"AltSlash","SlashEffectAlt"},
            {"Great Slash","SlashEffect"},
            {"Cyclone Slash","SlashEffect"},
            {"Dash Slash","SlashEffect"},
            {"WallSlash","SlashEffect"},
            {"DownSlash","DownSlashEffect"},
            {"UpSlash","UpSlashEffect"},
            {"Sharp Shadow","SlashEffect"},
        };

        private readonly List<float> runspeedvalue = new List<float>() { -8.3f, -10f };
        private readonly List<float> attackcooldownvalue = new List<float>() { 0.41f, 0.25f };
        private readonly List<float> healthvalue = new List<float>() { 1150f, 1800f };

        //buncha objects
        GameObject screamheads;
        GameObject screambase;
        GameObject screamorbs;

        private void Start()
        {
            On.HealthManager.TakeDamage += TakeDamageEvent;
            On.HutongGames.PlayMaker.FsmStateAction.ctor += Hotfix;
            CreateVessel();
        }

        private void Hotfix(On.HutongGames.PlayMaker.FsmStateAction.orig_ctor orig, FsmStateAction self)
        {
            orig(self);
            self.Reset();
        }

        private void OnDisable()
        {
            On.HealthManager.TakeDamage -= TakeDamageEvent;
            On.HutongGames.PlayMaker.FsmStateAction.ctor -= Hotfix;
        }

        private void CreateVessel()
        {
            SlashFsms.Clear();
            //create knight clone?
            GameObject clone = Instantiate(ResourceLoader.hkprime);
            clone.layer = (int)PhysLayers.ENEMIES;
            clone.tag = "";

            tk2dSprite sprite = clone.GetComponent<tk2dSprite>();
            tk2dSpriteAnimator animator = clone.GetComponent<tk2dSpriteAnimator>();
            animator.DefaultClipId = 0;
            animator.Library = HeroController.instance.GetComponent<tk2dSpriteAnimator>().Library;

            BoxCollider2D boxCollider = clone.GetComponent<BoxCollider2D>();
            boxCollider.size = new Vector2(0.4554f, 1.1698f);
            boxCollider.offset = new Vector2(0, -0.85f);
            boxCollider.enabled = true;

            clone.transform.position = vesselspawnpos;

            foreach (Transform child in clone.transform)
            {
                Destroy(child.gameObject);
            }
            foreach (PlayMakerFSM fsm in clone.GetComponents<PlayMakerFSM>())
            {
                Destroy(fsm);
            }
            Destroy(clone.GetComponent<ConstrainPosition>());
            Destroy(clone.GetComponent<PlayMakerFixedUpdate>());

            clone.GetComponent<HealthManager>().IsInvincible = false;
            clone.GetComponent<HealthManager>().OnDeath += Death;

            clone.SetActive(true);

            //corpse stuff
            Destroy(clone.GetComponent<EnemyDeathEffectsUninfected>());

            //effects and attacks
            GameObject attacks = Instantiate(HeroController.instance.gameObject.Child("Attacks"));
            PurgeFsms(attacks);
            attacks.transform.parent = clone.transform;
            attacks.transform.localPosition = new Vector3(0, 0, 0);
            attacks.transform.localScale = new Vector3(1, 1, 1);

            GameObject effects = Instantiate(HeroController.instance.gameObject.Child("Effects"));
            effects.transform.parent = clone.transform;
            effects.transform.localPosition = new Vector3(0, 0, 0);
            effects.transform.localScale = new Vector3(1, 1, 1);
            effects.SetActiveChildren(false);

            GameObject sounds = Instantiate(HeroController.instance.gameObject.Child("Sounds"));
            sounds.transform.parent = clone.transform;
            sounds.transform.localPosition = new Vector3(0, 0, 0);
            sounds.transform.localScale = new Vector3(1, 1, 1);

            GameObject focuseffects = Instantiate(HeroController.instance.gameObject.Child("Focus Effects"));
            focuseffects.transform.parent = clone.transform;
            focuseffects.transform.localPosition = new Vector3(0, 0, 0);
            focuseffects.transform.localScale = new Vector3(1, 1, 1);

            furybursteffect = Instantiate(HeroController.instance.gameObject.Child("Charm Effects").Child("Rage Burst Effect"));
            furybursteffect.transform.parent = clone.transform;
            furybursteffect.transform.localPosition = new Vector3(0, 0, 0);

            GameObject deatheffects = Instantiate(HeroController.instance.gameObject.Child("Hero Death"));
            headobject = deatheffects.GetComponent<PlayMakerFSM>().GetAction<CreateObject>("Head Right").gameObject.Value;
            Destroy(deatheffects.GetComponent<PlayMakerFSM>());
            deatheffects.transform.parent = clone.transform;
            deatheffects.transform.localPosition = new Vector3(0, 0, 0);
            deatheffects.Child("Dream Burst Pt").SetActive(false);
            deatheffects.Child("perma_death_looping_effects").SetActive(true);
            deatheffects.GetComponent<tk2dSpriteAnimator>().DefaultClipId = 230;

            GameObject spells = Instantiate(HeroController.instance.gameObject.Child("Spells"));
            spells.transform.parent = clone.transform;
            spells.transform.localPosition = new Vector3(0, 0, 0);
            spells.name = "Spells";

            //prepare Scream
            screamheads = spells.Child("Scr Heads 2");
            ReplaceDamage(screamheads);
            Destroy(screamheads.LocateMyFSM("Deactivate on Hit"));
            screamheads.transform.localScale = new Vector3(2, 2, 2);
            screambase = spells.Child("Scr Base 2");
            screamorbs = spells.Child("Scr Orbs 2");

            //prepare dive
            GameObject qslam = spells.Child("Q Slam 2");
            GameObject qmega = spells.Child("Q Mega");
            ReplaceDamage(qslam);
            ReplaceDamage(qmega);

            foreach (Transform child in attacks.transform)
            {
                child.gameObject.SetActive(true);

                child.gameObject.layer = (int)PhysLayers.ENEMY_ATTACK;

                foreach (Transform child2 in child.transform)
                {
                    child2.gameObject.layer = (int)PhysLayers.ENEMY_ATTACK;
                    //dude i dont know how to do this any other way im so sorry
                    foreach (Transform child3 in child2.transform)
                    {
                        child3.gameObject.layer = (int)PhysLayers.ENEMY_ATTACK;
                    }
                }

                child.gameObject.AddComponent<DamageHero>();

                PlayMakerFSM slashcontrol = child.gameObject.AddComponent<PlayMakerFSM>();

                FsmEvent slashevent = slashcontrol.CreateFsmEvent("SLASH");
                FsmEvent finishedevent = slashcontrol.CreateFsmEvent("FINISHED");

                FsmOwnerDefault ownerdefault = new FsmOwnerDefault();
                ownerdefault.GameObject = child.gameObject;
                ownerdefault.OwnerOption = OwnerDefaultOption.UseOwner;

                FsmEventTarget eventtarget = new FsmEventTarget();
                eventtarget.target = FsmEventTarget.EventTarget.Self;
                eventtarget.gameObject = ownerdefault;
                eventtarget.sendToChildren = false;
                eventtarget.excludeSelf = false;
                eventtarget.fsmComponent = slashcontrol;
                eventtarget.fsmName = "FSM";

                slashcontrol.Fsm.States = slashcontrol.Fsm.States.RemoveFirst(x => x.Name == "State 1").ToArray();

                FsmState slashstate = slashcontrol.CreateState("Slash");
                string name = SlashEffectAnims[child.gameObject.name];
                slashstate.AddAction(new Tk2dPlayAnimationWithEvents { animationCompleteEvent = finishedevent, gameObject = ownerdefault , clipName = name});
                slashstate.AddAction(new SetMeshRenderer { gameObject = ownerdefault, active = true });
                slashstate.AddAction(new SetPolygonCollider { active = true, gameObject = ownerdefault });

                FsmState inactivestate = slashcontrol.CreateState("Inactive");
                inactivestate.AddAction(new SetMeshRenderer { gameObject = ownerdefault, active = false});
                inactivestate.AddAction(new SetPolygonCollider { active = false, gameObject = ownerdefault });

                slashstate.AddTransition(finishedevent, "Inactive");
                inactivestate.AddTransition(slashevent, "Slash");

                child.gameObject.SetActive(true);

                tk2dSpriteAnimator Animator = child.GetComponent<tk2dSpriteAnimator>();
                if (Animator != null)
                {
                    Animator.Resume();
                }
                slashcontrol.SetState("Inactive");

                AddParryFsm(child.gameObject);

                SlashFsms.Add(child.gameObject.name, slashcontrol);

                if (child.gameObject.name == "Sharp Shadow")
                {
                    child.gameObject.SetActive(false);
                }
            }

            //
            Rigidbody2D rigidbody = clone.GetComponent<Rigidbody2D>();
            rigidbody.centerOfMass = new Vector2(0, -0.85f);
            rigidbody.gravityScale = 1;
            rigidbody.bodyType = RigidbodyType2D.Dynamic;
            rigidbody.collisionDetectionMode = CollisionDetectionMode2D.Discrete;

            clone.AddComponent<SetZ>();

            ConstrainPosition constrain = clone.AddComponent<ConstrainPosition>();
            constrain.constrainX = true;
            constrain.constrainY = true;
            constrain.xMin = 28;
            constrain.xMax = 70;
            constrain.yMin = 7.4129f;
            constrain.yMax = 17;

            int option = Kindred_Spirit.DreamToggle ? 1 : 0;
            clone.GetComponent<HealthManager>().hp = (int)healthvalue[option];

            //adding ranges            
            CreateRange(clone, "Slash Range", new Vector2(2.7f,1.5f), new Vector2(-1.6f,0));
            CreateRange(clone, "Upslash Range", new Vector2(2f, 2.85f), new Vector2(0, 1.225f));
            CreateRange(clone, "Downslash Range", new Vector2(2f, 2f), new Vector2(0, -2f));
            CreateRange(clone, "Scream Range", new Vector2(6f, 5f), new Vector2(0, 6f));
            CreateRange(clone, "Quake Range", new Vector2(6f, 15f), new Vector2(0, -15f));
            CreateRange(clone, "Fireball Range", new Vector2(15f, 3f), new Vector2(-15f, 0f));

            //add dreamnail reactions
            if (Kindred_Spirit.DreamToggle)
            {
                PlayMakerFSM dnreject = clone.AddComponent(ResourceLoader.hkprime.LocateMyFSM("Dreamnail Reject"));
                clone.AddComponent<DreamNailReject>();
                //i dunno why this works but whatever
                dnreject.enabled = false;
                dnreject.enabled = true;
                dnreject.SetState("Init");
            } else
            {
                EnemyDreamnailReaction dnr = clone.AddComponent<EnemyDreamnailReaction>();
                dnr.SetConvoTitle("FALLEN_VESSEL_DN");
            }


            Vessel = clone;
            Vessel.transform.SetScaleX(Vessel.transform.GetScaleX() * -1);
            SetDamage(1);

            

            FsmSetup();
        }

        private void ReplaceDamage(GameObject parent)
        {
            foreach (Transform obj in parent.transform)
            {
                foreach (PlayMakerFSM fsm in obj.gameObject.GetComponentsInChildren<PlayMakerFSM>(true))
                {
                    if (fsm.FsmName == "damages_enemy")
                    {
                        DamageHero dmg = fsm.gameObject.AddComponent<DamageHero>();
                        if (Kindred_Spirit.AbsoluteToggle)
                        {
                            dmg.damageDealt = 2;
                        }
                        Destroy(fsm);
                    }
                }
            }
        }

        private void QuakeBegin()
        {
            if (Vessel)
            {
                GameObject spells = Vessel.Child("Spells");
                GameObject quakecharge = spells.Child("Q Charge");
                GameObject qflash = spells.Child("Q Flash Start");

                quakecharge.SetActive(true);
                qflash.SetActive(true);
            }
        }

        private void QuakeFall()
        {
            if (Vessel)
            {
                GameObject spells = Vessel.Child("Spells");
                GameObject quakecharge = spells.Child("Q Charge");
                GameObject quakefall = spells.Child("Q Trail 2");

                quakecharge.SetActive(false);
                quakefall.SetActive(true);
            }
        }

        private void QuakeLand()
        {
            //you know i wrote this whole thing and i still dont know why i formatted it like this. i will keep it for fun.
            if (Vessel)
            {
                GameObject spells = Vessel.Child("Spells");
                GameObject quakefall = spells.Child("Q Trail 2");
                GameObject quakeslam = spells.Child("Q Slam 2");
                GameObject quakeflash = spells.Child("Q Flash Slam");

                quakefall.SetActive(false);
                quakeslam.SetActive(true);
                quakeflash.SetActive(true);
            }
        }

        private void QuakeMega()
        {
            if (Vessel)
            {
                GameObject spells = Vessel.Child("Spells");
                GameObject quakeslamlines = spells.Child("Q Slam Lines");
                GameObject quakemega = spells.Child("Q Mega");
                GameObject quakepillar = spells.Child("Q Pillar");
                GameObject qorbs = spells.Child("Q Orbs");
                GameObject qorbs2 = spells.Child("Q Orbs 2");

                PlayMakerFSM fsm = quakemega.LocateMyFSM("Hit Box Control");
                if (fsm)
                {
                    fsm.FsmName = "HBC";
                }
                quakemega.SetActive(true);
                quakeslamlines.SetActive(true);
                quakepillar.SetActive(true);

                qorbs.GetComponent<PlayMakerFSM>().SendEvent("PLAY");
                qorbs2.GetComponent<PlayMakerFSM>().SendEvent("PLAY");
            }
        }

        private void QuakeEnd()
        {
            if (Vessel)
            {
                GameObject spells = Vessel.Child("Spells");
                GameObject quakemega = spells.Child("Q Mega");
                
                quakemega.SetActive(false);
            }
        }

        private void Scream()
        {
            if (Vessel)
            {
                screamheads.SetActive(true);
                screambase.SetActive(true);
                screamorbs.GetComponent<PlayMakerFSM>().SendEvent("PLAY");

                GameObject roar = HeroController.instance.spellControl.GetAction<CreateObject>("Scream Burst 2", 3).gameObject.Value;
                roar = Instantiate(roar);
                roar.transform.position = Vessel.transform.position;
                roar.LocateMyFSM("emitter").FsmVariables.GetFsmBool("No Waves").Value = true;

            }
        }

        private void DoubleJump()
        {
            if (Vessel)
            {
                GameObject effects = Vessel.Child("Effects(Clone)");
                effects.Child("Double J Wings").SetActive(true);
                effects.Child("Double J Feather").GetComponent<ParticleSystem>().Emit(15);
            }
        }

        private void Death()
        {
            if (Kindred_Spirit.DreamToggle)
            {
                Vessel.LocateMyFSM("Control").SendEvent("Die");
            } else
            {
                Vessel.LocateMyFSM("Control").SendEvent("DEATH");
            }
            Vessel.GetComponent<HealthManager>().OnDeath -= Death;
        }

        private void FsmSetup()
        {
            int option = Kindred_Spirit.DreamToggle ? 1 : 0;

            PlayMakerFSM control = Vessel.AddComponent<PlayMakerFSM>();
            control.FsmName = "Control";

            FsmOwnerDefault ownerdefault = new FsmOwnerDefault();
            ownerdefault.GameObject = gameObject;
            ownerdefault.OwnerOption = OwnerDefaultOption.UseOwner;

            //stuncontrol
            PlayMakerFSM stuncontrol = Vessel.AddComponent(ResourceLoader.hkprime.LocateMyFSM("Stun Control"));
            stuncontrol.FsmVariables.GetFsmInt("Hits Total").Value = 0;
            stuncontrol.enabled = false;
            stuncontrol.enabled = true;
            stuncontrol.SetState("Init");

            //extras
            FsmGameObject herogameobject = HeroController.instance.gameObject;
            FsmGameObject fireballobject = HeroController.instance.spellControl.GetAction<SpawnObjectFromGlobalPool>("Fireball 1", 3).gameObject;
            if (Kindred_Spirit.DreamToggle)
            {
                fireballobject = HeroController.instance.spellControl.GetAction<SpawnObjectFromGlobalPool>("Fireball 2", 3).gameObject;
            }
            FsmOwnerDefault cameraparent = ResourceLoader.hkprime.gameObject.LocateMyFSM("Control").GetAction<SetFsmBool>("Stun Start", 16).gameObject;
            FsmGameObject stungameobject = ResourceLoader.hkprime.gameObject.LocateMyFSM("Control").GetAction<SpawnObjectFromGlobalPool>("Stun Start", 17).gameObject;
            FsmGameObject areatitleobject = ResourceLoader.megamoss.gameObject.LocateMyFSM("Mossy Control").GetAction<SetGameObject>("Title?", 1).gameObject;
            PlayMakerFSM titlefsm = areatitleobject.Value.gameObject.GetComponent<PlayMakerFSM>();
            FsmObject snapshot = ResourceLoader.megamoss.gameObject.LocateMyFSM("Mossy Control").GetAction<TransitionToAudioSnapshot>("Music", 1).snapshot;
            FsmObject musiccue = ResourceLoader.megamoss.gameObject.LocateMyFSM("Mossy Control").GetAction<ApplyMusicCue>("Music", 3).musicCue;
            AudioClip deathsound = (AudioClip)HeroController.instance.gameObject.Child("Hero Death").LocateMyFSM("Hero Death Anim").GetAction<AudioPlayerOneShotSingle>("Start", 4).audioClip.Value;
            hurtsound = (AudioClip)HeroController.instance.gameObject.Child("Hero Death").LocateMyFSM("Hero Death Anim").GetAction<AudioPlayerOneShotSingle>("Start", 5).audioClip.Value;
            AudioClip screamsound = (AudioClip)HeroController.instance.spellControl.GetState("Scream Antic2").GetAction<AudioPlay>().oneShotClip.Value;
            AudioClip quakepreparesound = (AudioClip)HeroController.instance.spellControl.GetState("Quake Antic").GetAction<AudioPlay>().oneShotClip.Value;
            AudioClip quakefallsound = (AudioClip)HeroController.instance.spellControl.GetState("Q2 Effect").GetAction<AudioPlaySimple>().oneShotClip.Value;
            AudioClip quakelandsound = (AudioClip)HeroController.instance.spellControl.GetState("Q2 Land").GetAction<AudioPlay>().oneShotClip.Value;
            MusicCue deathmusic = GameManager.instance.AudioManager.CurrentMusicCue;
            AudioManager audiomanager = GameManager.instance.AudioManager;


            //events
            FsmEvent slashevent = control.CreateFsmEvent("SLASH");
            FsmEvent slashaltevent = control.CreateFsmEvent("SLASHALT");
            FsmEvent upslashevent = control.CreateFsmEvent("UPSLASH");
            FsmEvent finishedevent = control.CreateFsmEvent("FINISHED");
            FsmEvent turnevent = control.CreateFsmEvent("TURN");
            FsmEvent approachevent = control.CreateFsmEvent("APPROACH");
            FsmEvent jumpchaseevent = control.CreateFsmEvent("JUMPCHASE");
            FsmEvent actionevent = control.CreateFsmEvent("ACTION");
            FsmEvent endjumpevent = control.CreateFsmEvent("ENDJUMP");
            FsmEvent dashawayevent = control.CreateFsmEvent("DASHAWAY");
            FsmEvent fallevent = control.CreateFsmEvent("FALL");
            FsmEvent fireballevent = control.CreateFsmEvent("FIREBALL");
            FsmEvent endstunevent = control.CreateFsmEvent("END");
            FsmEvent endintroevent = control.CreateFsmEvent("ENDINTRO");
            FsmEvent returnevent = control.CreateFsmEvent("RETURN");
            FsmEvent focusevent = control.CreateFsmEvent("FOCUS");
            FsmEvent screamevent = control.CreateFsmEvent("SCREAM");
            FsmEvent quakeevent = control.CreateFsmEvent("QUAKE");
            FsmEvent shadedashevent = control.CreateFsmEvent("SHADEDASH");
            FsmEvent animendevent = control.CreateFsmEvent("ANIM END");
            FsmEvent cancelevent = control.CreateFsmEvent("CANCEL");
            FsmEvent doublejumpevent = control.CreateFsmEvent("DOUBLEJUMP");
            FsmEvent blankevent = new FsmEvent("");

            //vars
            FsmFloat idletimer = control.CreateFsmFloat("Idle Timer", 0);
            FsmFloat idletime = control.CreateFsmFloat("Idle Time", 0.1f);
            FsmFloat xscale = control.CreateFsmFloat("X Scale", Vessel.gameObject.transform.GetScaleX());
            FsmFloat runspeed = control.CreateFsmFloat("Run Speed", runspeedvalue[option]);
            FsmFloat jumpspeed = control.CreateFsmFloat("Jump Speed", 16.65f);
            FsmFloat runspeedvelo = control.CreateFsmFloat("Run Speed Velo", 8.3f);
            FsmFloat approachtimer = control.CreateFsmFloat("Approach Timer", 0f);
            FsmFloat approachtime = control.CreateFsmFloat("Approach Time", 2f);
            FsmFloat approachtimemin = control.CreateFsmFloat("Approach Time Min", 1.5f);
            FsmFloat approachtimemax = control.CreateFsmFloat("Approach Time Max", 2.5f);
            FsmFloat slashdelaytime = control.CreateFsmFloat("Slash Delay Time", 0.2f);
            FsmFloat slashdelaymin = control.CreateFsmFloat("Slash Delay Min", 0.05f);
            FsmFloat slashdelaymax = control.CreateFsmFloat("Slash Delay Max", 0.12f);
            FsmFloat jumptimemin = control.CreateFsmFloat("Jump Time Min", 0.2f);
            FsmFloat jumptimemax = control.CreateFsmFloat("Jump Time Max", 0.33f);
            FsmFloat jumptime = control.CreateFsmFloat("Jump Time", 0f);
            FsmFloat dashspeed = control.CreateFsmFloat("Dash Speed", -20f);
            FsmFloat dashtime = control.CreateFsmFloat("Dash Time", 0.25f);
            FsmFloat dashspeedvelo = control.CreateFsmFloat("Dash Speed Velo", 0f);
            FsmFloat fireballrecoilvelo = control.CreateFsmFloat("Fireball Recoil Velo", 0f);
            FsmFloat slashcooldown = control.CreateFsmFloat("Slash Cooldown", attackcooldownvalue[option]);
            FsmFloat yspeed = control.CreateFsmFloat("Y speed", 50);

            FsmInt ctdashaway = control.GetOrCreateInt("Ct Dashaway");
            FsmInt ctapproach = control.GetOrCreateInt("Ct Approach");
            FsmInt ctjump = control.GetOrCreateInt("Ct Jump");
            FsmInt ctfireball = control.GetOrCreateInt("Ct Fireball");
            FsmInt ctfocus = control.GetOrCreateInt("Ct Focus");
            FsmInt ctshade = control.GetOrCreateInt("Ct Shadedash");
            FsmInt msdashaway = control.GetOrCreateInt("Ms Dashaway");
            FsmInt msapproach = control.GetOrCreateInt("Ms Approach");
            FsmInt msjump = control.GetOrCreateInt("Ms Jump");
            FsmInt msfireball = control.GetOrCreateInt("Ms Fireball");
            FsmInt msfocus = control.GetOrCreateInt("Ms Focus");
            FsmInt msshade = control.GetOrCreateInt("Ms Shadedash");

            FsmBool facingright = control.CreateBool("Facing Right?");
            FsmBool heroisright = control.CreateBool("Hero Is Right?");
            FsmBool altbool = control.CreateBool("AltSlash?");
            FsmBool slashrange = control.CreateBool("Slash Range");
            FsmBool upslashrange = control.CreateBool("Upslash Range");
            FsmBool downslashrange = control.CreateBool("Downslash Range");
            FsmBool screamrange = control.CreateBool("Scream Range");
            FsmBool quakerange = control.CreateBool("Quake Range");
            FsmBool screamcooldown = control.CreateBool("Scream Cooldown");
            FsmBool fireballcooldown = control.CreateBool("Fireball Cooldown");
            FsmBool shadowdashcooldown = control.CreateBool("ShadowDash Cooldown");
            FsmBool quakecooldown = control.CreateBool("Quake Cooldown");
            FsmBool doublejcooldown = control.CreateBool("Double J Cooldown");
            FsmBool fireballrange = control.CreateBool("Fireball Range");

            FsmString clipname = control.FsmVariables.GetFsmString("FallClip");
            clipname.Value = "FallGV";

            FsmGameObject fireballattackobject = control.CreateFsmGameObject("Fireball Object");
            FsmGameObject stunobjectstore = control.CreateFsmGameObject("Stun Object");

            FsmBool[] isrightarray = new FsmBool[2];
            isrightarray[0] = heroisright;
            isrightarray[1] = facingright;

            //longer, but cleaner for me.

            List<FsmBool> truefalselist = new List<FsmBool>() { true, false };
            List<FsmBool> falsetruelist = new List<FsmBool>() { false, true };
            List<FsmEvent> actioneventlist = new List<FsmEvent>() { approachevent, jumpchaseevent, dashawayevent, fireballevent, focusevent };
            List<FsmInt> trackingintlist = new List<FsmInt>() { ctapproach, ctjump, ctdashaway, ctfireball, ctfocus };
            List<FsmInt> missintlist = new List<FsmInt>() { msapproach, msjump, msdashaway, msfireball, msfocus };
            List<FsmInt> maxlist = new List<FsmInt>() { 2, 2, 2, 1, 1 };
            List<FsmInt> misslist = new List<FsmInt>() { 5, 5, 5, 5, 10 };
            List<FsmFloat> actionweightlist = new List<FsmFloat>() { 0.275f, 0.175f, 0.275f, 0.175f, 0.1f };

            if (Kindred_Spirit.DreamToggle)
            {
                actioneventlist.Add(shadedashevent);
                trackingintlist.Add(ctshade);
                missintlist.Add(msshade);
                maxlist.Add(1);
                misslist.Add(8);
                actionweightlist.Add(0.1f);
                actionweightlist[0].Value -= 0.1f;
            }

            FsmFloat[] actionweightarray = actionweightlist.ToArray();
            FsmInt[] missarray = misslist.ToArray();
            FsmInt[] maxarray = maxlist.ToArray();
            FsmInt[] missints = missintlist.ToArray();
            FsmInt[] trackingints = trackingintlist.ToArray();
            FsmEvent[] actionarray = actioneventlist.ToArray();
            FsmBool[] falsetruearray = falsetruelist.ToArray();
            FsmBool[] truefalsearray = truefalselist.ToArray();

            FsmEvent[] approachscreamevents = new FsmEvent[2]; approachscreamevents[0] = screamevent; approachscreamevents[1] = finishedevent;
            FsmFloat[] approachscreamweights = new FsmFloat[2]; approachscreamweights[0] = 0.33f; approachscreamweights[1] = 0.67f;

            FsmBool[] screambools = new FsmBool[2]; screambools[0] = screamcooldown; screambools[1] = screamrange;
            FsmBool[] quakebools = new FsmBool[2]; quakebools[0] = quakecooldown; quakebools[1] = quakerange;
            FsmBool[] fireballbools = new FsmBool[3]; fireballbools[0] = fireballrange; fireballbools[1] = Kindred_Spirit.DreamToggle; fireballbools[2] = fireballcooldown;
            FsmBool[] truetruearray = new FsmBool[2]; truetruearray[0] = true; truetruearray[1] = true;
            //this one was just  for fun, actually
            FsmBool[] truetruefalsearray = new FsmBool[3]; truetruefalsearray[0] = true; truetruefalsearray[1] = true; truetruefalsearray[2] = false;
            //cd vals
            float fireballcd = 1f;
            float screamcd = 12f;
            float quakecd = 15f;
            float doublejumpcd = 10f;

            //lil extra
            bool introJump = false;
            if (Kindred_Spirit.DreamToggle)
            {
                fireballcd = 10f;
            }
            if (Kindred_Spirit.AbsoluteToggle)
            {
                fireballcd = 0f;
                screamcd = 0f;
                quakecd = 0f;
                doublejumpcd = 0f;
            }

            //stun connect
            HealthManager hp = control.gameObject.GetComponent<HealthManager>();

            //states

            FsmState idlestate = control.CreateState("Idle");
            idlestate.AddMethod(() => { Vessel.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Dynamic; StopSound("FootstepsRun"); Vessel.GetComponent<BoxCollider2D>().enabled = true; QuakeEnd(); });
            idlestate.AddAction(new SetVelocity2d { gameObject = ownerdefault, x = 0f, everyFrame = false, vector = new Vector2(0, 0), y = 0f });
            idlestate.AddAction(new Tk2dPlayAnimationWithEvents { gameObject = ownerdefault, clipName = "IdleGV" });
            idlestate.AddAction(new SetFloatValue { floatVariable = idletimer, floatValue = 0, everyFrame = false });
            idlestate.AddAction(new FloatAdd { add = 1f, floatVariable = idletimer, everyFrame = true, perSecond = true });
            idlestate.AddAction(new FloatCompare { float1 = idletimer, float2 = idletime, greaterThan = actionevent, everyFrame = true });
            idlestate.AddAction(new GetScale { gameObject = ownerdefault, xScale = xscale, everyFrame = false, yScale = 0f, zScale = 0f, space = 0, vector = new Vector3(0, 0) });
            idlestate.AddAction(new FloatTestToBool { float1 = xscale, float2 = 0f, greaterThanBool = facingright, everyFrame = false, equalBool = false, lessThanBool = false, tolerance = 0f });
            idlestate.AddAction(new CheckTargetDirection { gameObject = ownerdefault, target = herogameobject, rightBool = false, leftBool = heroisright, everyFrame = true, aboveBool = false, aboveEvent = blankevent, belowBool = false, belowEvent = blankevent, leftEvent = blankevent, rightEvent = blankevent });
            idlestate.AddAction(new BoolTestMulti { boolVariables = isrightarray, boolStates = falsetruearray, trueEvent = turnevent, everyFrame = true, storeResult = false, });
            idlestate.AddAction(new BoolTestMulti { boolVariables = isrightarray, boolStates = truefalsearray, trueEvent = turnevent, everyFrame = true, storeResult = false });
            idlestate.AddAction(new BoolTest { boolVariable = slashrange, everyFrame = true, isTrue = slashevent });

            FsmState actionchoicestate = control.CreateState("Action Choice");
            actionchoicestate.AddMethod(() => { if (HeroController.instance.cState.spellQuake) { control.SendEvent("DASHAWAY"); } });
            actionchoicestate.AddAction(new SendRandomEventV3 { events = actionarray, weights = actionweightarray, trackingInts = trackingints, missedMax = missarray, eventMax = maxarray, trackingIntsMissed = missints });

            FsmState turnanimstate = control.CreateState("Turn Anim");
            turnanimstate.AddAction(new Tk2dPlayAnimationWithEvents { gameObject = ownerdefault, clipName = "TurnGV", animationCompleteEvent = finishedevent });
            turnanimstate.AddAction(new FloatAdd { add = 1f, floatVariable = idletimer, everyFrame = true, perSecond = true });

            FsmState turnflipstate = control.CreateState("Turn Flip");
            turnflipstate.AddAction(new FlipScale { gameObject = ownerdefault, flipHorizontally = true, flipVertically = false, everyFrame = false, lateUpdate = false });
            turnflipstate.AddAction(new Tk2dPlayAnimation { gameObject = ownerdefault, clipName = "IdleGV", animLibName = "" });

            FsmState slashdelaystate = control.CreateState("Slash Delay");
            slashdelaystate.AddAction(new RandomFloat { min = slashdelaymin, max = slashdelaymax, storeResult = slashdelaytime });
            slashdelaystate.AddAction(new Wait { finishEvent = finishedevent, realTime = false, time = slashdelaytime });

            FsmState slashoption = control.CreateState("Slash Choice");
            slashoption.AddAction(new BoolTest { boolVariable = upslashrange, everyFrame = false, isTrue = upslashevent });
            slashoption.AddAction(new BoolTest { boolVariable = altbool, everyFrame = false, isFalse = slashevent, isTrue = slashaltevent });

            FsmState slashstate = control.CreateState("Slash");
            slashstate.AddAction(new SetBoolValue { boolVariable = altbool, boolValue = true });
            slashstate.AddAction(new Tk2dPlayAnimationWithEvents { gameObject = ownerdefault, clipName = "SlashGV" });
            slashstate.AddAction(new SendEvent { delay = slashcooldown, eventTarget = control.Fsm.EventTarget, everyFrame = false, sendEvent = finishedevent });
            slashstate.AddMethod(() => { SlashFsms["Slash"].SetState("Slash"); SlashFsms["Slash"].gameObject.LocateMyFSM("Clash").SetState("Initiate"); SlashFsms["Slash"].gameObject.GetComponent<AudioSource>().pitch = 1f; SlashFsms["Slash"].gameObject.GetComponent<AudioSource>().Play(); });

            FsmState altslashstate = control.CreateState("SlashAlt");
            altslashstate.AddAction(new SetBoolValue { boolVariable = altbool, boolValue = false });
            altslashstate.AddAction(new Tk2dPlayAnimationWithEvents { animationCompleteEvent = finishedevent, gameObject = ownerdefault, clipName = "SlashAltGV" });
            altslashstate.AddAction(new SendEvent { delay = slashcooldown, eventTarget = control.Fsm.EventTarget, everyFrame = false, sendEvent = finishedevent });
            altslashstate.AddMethod(() => { SlashFsms["AltSlash"].SetState("Slash"); SlashFsms["AltSlash"].gameObject.LocateMyFSM("Clash").SetState("Initiate"); SlashFsms["AltSlash"].gameObject.GetComponent<AudioSource>().pitch = 1f; SlashFsms["AltSlash"].gameObject.GetComponent<AudioSource>().Play(); });

            FsmState upslashstate = control.CreateState("UpSlash");
            upslashstate.AddAction(new Tk2dPlayAnimationWithEvents { gameObject = ownerdefault, clipName = "UpSlashGV", animationCompleteEvent = finishedevent });
            upslashstate.AddAction(new SendEvent { delay = slashcooldown, eventTarget = control.Fsm.EventTarget, everyFrame = false, sendEvent = finishedevent });
            upslashstate.AddMethod(() => { SlashFsms["UpSlash"].SetState("Slash"); SlashFsms["UpSlash"].gameObject.LocateMyFSM("Clash").SetState("Initiate"); SlashFsms["UpSlash"].gameObject.GetComponent<AudioSource>().pitch = 1f; SlashFsms["UpSlash"].gameObject.GetComponent<AudioSource>().Play(); });

            FsmState downslashstate = control.CreateState("DownSlash");
            downslashstate.AddAction(new Tk2dPlayAnimationWithEvents { gameObject = ownerdefault, clipName = "DownSlashGV", animationCompleteEvent = finishedevent });
            downslashstate.AddAction(new SendEvent { delay = slashcooldown, eventTarget = control.Fsm.EventTarget, everyFrame = false, sendEvent = finishedevent });
            downslashstate.AddMethod(() => { SlashFsms["DownSlash"].SetState("Slash"); SlashFsms["DownSlash"].gameObject.LocateMyFSM("Clash").SetState("Initiate"); SlashFsms["DownSlash"].gameObject.GetComponent<AudioSource>().pitch = 1f; SlashFsms["DownSlash"].gameObject.GetComponent<AudioSource>().Play(); });

            FsmState postslashstate = control.CreateState("Post Slash");
            postslashstate.AddAction(new Tk2dPlayAnimation { gameObject = ownerdefault, animLibName = "", clipName = "FallGV" });
            postslashstate.AddAction(new CheckCollisionSide { bottomHitEvent = finishedevent, ignoreTriggers = false, otherLayer = false, otherLayerNumber = 0, topHit = false, rightHit = false, bottomHit = false, leftHit = false });
            postslashstate.AddAction(new CheckCollisionSideEnter { bottomHitEvent = finishedevent, ignoreTriggers = false, otherLayer = false, otherLayerNumber = 0, topHit = false, rightHit = false, bottomHit = false, leftHit = false });

            FsmState approachstate = control.CreateState("Approach");
            approachstate.AddAction(new GetScale { gameObject = ownerdefault, xScale = xscale, everyFrame = false, yScale = 0f, zScale = 0f, space = 0, vector = new Vector3(0, 0) });
            approachstate.AddAction(new FloatOperator { float1 = runspeed, float2 = xscale, operation = (FloatOperator.Operation)2, storeResult = runspeedvelo, everyFrame = false });
            approachstate.AddAction(new Tk2dPlayAnimation { gameObject = ownerdefault, animLibName = "", clipName = "RunGV" });
            approachstate.AddAction(new SetVelocity2d { gameObject = ownerdefault, x = runspeedvelo, everyFrame = false, vector = new Vector2(0, 0), y = 0f });
            approachstate.AddAction(new FloatCompare { float1 = approachtimer, float2 = approachtime, greaterThan = finishedevent, everyFrame = true });
            approachstate.AddAction(new FloatAdd { add = 1f, floatVariable = approachtimer, everyFrame = true, perSecond = true });
            approachstate.AddAction(new BoolTest { boolVariable = slashrange, everyFrame = true, isTrue = slashevent });
            approachstate.AddAction(new BoolTest { boolVariable = upslashrange, everyFrame = true, isTrue = slashevent });
            approachstate.AddAction(new FloatTestToBool { float1 = xscale, float2 = 0f, greaterThanBool = facingright, everyFrame = false, equalBool = false, lessThanBool = false, tolerance = 0f }); ;
            approachstate.AddAction(new CheckTargetDirection { gameObject = ownerdefault, target = herogameobject, rightBool = false, leftBool = heroisright, everyFrame = true, aboveBool = false, aboveEvent = blankevent, belowBool = false, belowEvent = blankevent, leftEvent = blankevent, rightEvent = blankevent });
            approachstate.AddAction(new BoolTestMulti { boolVariables = isrightarray, boolStates = falsetruearray, trueEvent = turnevent, everyFrame = true, storeResult = false, });
            approachstate.AddAction(new BoolTestMulti { boolVariables = isrightarray, boolStates = truefalsearray, trueEvent = turnevent, everyFrame = true, storeResult = false });
            approachstate.AddAction(new BoolTestMulti { boolVariables = screambools, boolStates = falsetruearray, trueEvent = screamevent, everyFrame = true, storeResult = false });

            FsmState approachturnstate = control.CreateState("Approach Turn");
            approachturnstate.AddAction(new Tk2dPlayAnimationWithEvents { gameObject = ownerdefault, clipName = "TurnGV", animationCompleteEvent = finishedevent });
            approachturnstate.AddAction(new FloatAdd { add = 1f, floatVariable = approachtimer, everyFrame = true, perSecond = true });

            FsmState approachturnflipstate = control.CreateState("Approach Turn Flip");
            approachturnflipstate.AddAction(new FlipScale { gameObject = ownerdefault, flipHorizontally = true, flipVertically = false, everyFrame = false, lateUpdate = false });

            FsmState approachbeginstate = control.CreateState("Approach Begin");
            approachbeginstate.AddMethod(() => { PlaySound("FootstepsRun"); });
            approachbeginstate.AddAction(new SetFloatValue { everyFrame = false, floatValue = 0f, floatVariable = approachtimer });
            approachbeginstate.AddAction(new RandomFloat { min = approachtimemin, max = approachtimemax, storeResult = approachtime });

            FsmState jumpstate = control.CreateState("Jump Chase");
            jumpstate.AddMethod(() => { PlaySound("Jump"); Vessel.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic; });
            jumpstate.AddAction(new GetScale { gameObject = ownerdefault, xScale = xscale, everyFrame = false, yScale = 0f, zScale = 0f, space = 0, vector = new Vector3(0, 0) });
            jumpstate.AddAction(new FloatOperator { float1 = runspeed, float2 = xscale, operation = (FloatOperator.Operation)2, storeResult = runspeedvelo, everyFrame = false });
            jumpstate.AddAction(new Tk2dPlayAnimation { gameObject = ownerdefault, animLibName = "", clipName = "AirborneGV" });
            jumpstate.AddAction(new SetVelocity2d { gameObject = ownerdefault, x = runspeedvelo, everyFrame = true, vector = new Vector2(0, 0), y = jumpspeed });
            jumpstate.AddAction(new RandomFloat { min = jumptimemin, max = jumptimemax, storeResult = jumptime });
            jumpstate.AddAction(new Wait { finishEvent = endjumpevent, realTime = false, time = jumptime });

            FsmState doublejumpcheckstate = control.CreateState("Double Jump Check");
            doublejumpcheckstate.AddMethod(() => { if (!Kindred_Spirit.DreamToggle) { control.SendEvent("CANCEL"); } });
            doublejumpcheckstate.AddAction(new Wait { finishEvent = doublejumpevent, realTime = false, time = 0.05f });

            FsmState doublejumpstate = control.CreateState("Double Jump");
            doublejumpstate.AddAction(new BoolTest { boolVariable = doublejcooldown, isTrue = endjumpevent });
            doublejumpstate.AddMethod(() => { if (!Kindred_Spirit.DreamToggle) { control.SetState("Fall"); } });
            doublejumpstate.AddMethod(() => { PlayOneShot(HeroController.instance.doubleJumpClip); Vessel.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic; SpellCD(control, "Double J Cooldown", doublejumpcd); DoubleJump(); });
            doublejumpstate.AddAction(new GetScale { gameObject = ownerdefault, xScale = xscale, everyFrame = false, yScale = 0f, zScale = 0f, space = 0, vector = new Vector3(0, 0) });
            doublejumpstate.AddAction(new FloatOperator { float1 = runspeed, float2 = xscale, operation = (FloatOperator.Operation)2, storeResult = runspeedvelo, everyFrame = false });
            doublejumpstate.AddAction(new Tk2dPlayAnimation { gameObject = ownerdefault, animLibName = "", clipName = "Double JumpGV" });
            doublejumpstate.AddAction(new SetVelocity2d { gameObject = ownerdefault, x = runspeedvelo, everyFrame = true, vector = new Vector2(0, 0), y = jumpspeed });
            doublejumpstate.AddAction(new RandomFloat { min = jumptimemin, max = jumptimemax, storeResult = jumptime });
            doublejumpstate.AddAction(new Wait { finishEvent = endjumpevent, realTime = false, time = jumptime });

            //shut up
            void setName()
            {
                clipname.Value = "FallGV"; if (introJump) { clipname.Value = "IntroJumpGV"; }; introJump = false;
            }

            FsmState fallstate = control.CreateState("Fall");
            fallstate.AddMethod(() => { Vessel.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Dynamic; setName(); });
            fallstate.AddAction(new Tk2dPlayAnimation { gameObject = ownerdefault, animLibName = "", clipName = clipname });
            fallstate.AddAction(new CheckCollisionSide { bottomHitEvent = finishedevent, ignoreTriggers = false, otherLayer = false, otherLayerNumber = 0, topHit = false, rightHit = false, bottomHit = false, leftHit = false });
            fallstate.AddAction(new CheckCollisionSideEnter { bottomHitEvent = finishedevent, ignoreTriggers = false, otherLayer = false, otherLayerNumber = 0, topHit = false, rightHit = false, bottomHit = false, leftHit = false });
            fallstate.AddAction(new SetVelocity2d { gameObject = ownerdefault, x = runspeedvelo, everyFrame = false, vector = new Vector2(0, 0), y = 0 });
            fallstate.AddAction(new BoolTest { boolVariable = downslashrange, everyFrame = true, isTrue = slashevent });
            fallstate.AddAction(new BoolTest { boolVariable = slashrange, everyFrame = true, isTrue = slashaltevent });
            fallstate.AddAction(new BoolTestMulti { boolVariables = screambools, boolStates = falsetruearray, trueEvent = screamevent, everyFrame = true, storeResult = false });
            fallstate.AddAction(new BoolTestMulti { boolVariables = quakebools, boolStates = falsetruearray, trueEvent = quakeevent, everyFrame = true, storeResult = false });
            fallstate.AddAction(new BoolTestMulti { boolVariables = fireballbools, boolStates = truetruefalsearray, trueEvent = fireballevent, everyFrame = true, storeResult = false });

            FsmState landstate = control.CreateState("Land");
            landstate.AddMethod(() => { PlaySound("Landing"); });
            landstate.AddAction(new Tk2dPlayAnimationWithEvents { animationCompleteEvent = finishedevent, gameObject = ownerdefault, clipName = "LandGV" });
            landstate.AddAction(new SetVelocity2d { gameObject = ownerdefault, x = 0, everyFrame = false, vector = new Vector2(0, 0), y = 0 });

            FsmState dashawaystate = control.CreateState("Dash Away");
            dashawaystate.AddMethod(() => { Vessel.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic; PlayDash(Vessel); PlaySound("Dash"); });
            dashawaystate.AddAction(new Tk2dPlayAnimationWithEvents { animationCompleteEvent = finishedevent, gameObject = ownerdefault, clipName = "DashGV" });
            dashawaystate.AddAction(new FlipScale { gameObject = ownerdefault, flipHorizontally = true, flipVertically = false, everyFrame = false, lateUpdate = false });
            dashawaystate.AddAction(new GetScale { gameObject = ownerdefault, xScale = xscale, everyFrame = false, yScale = 0f, zScale = 0f, space = 0, vector = new Vector3(0, 0) });
            dashawaystate.AddAction(new FloatOperator { float1 = dashspeed, float2 = xscale, operation = (FloatOperator.Operation)2, storeResult = dashspeedvelo, everyFrame = false });
            dashawaystate.AddAction(new SetVelocity2d { gameObject = ownerdefault, x = dashspeedvelo, everyFrame = false, vector = new Vector2(0, 0), y = 0 });
            dashawaystate.AddAction(new Wait { finishEvent = finishedevent, realTime = false, time = dashtime });

            FsmState shadowdashstate = control.CreateState("Shadow Dash");
            shadowdashstate.AddAction(new BoolTest { boolVariable = shadowdashcooldown, everyFrame = false, isTrue = finishedevent });
            shadowdashstate.AddMethod(() => { Vessel.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic; PlayShadowDash(); PlayOneShot(HeroController.instance.shadowDashClip); Vessel.GetComponent<BoxCollider2D>().enabled = false; SpellCD(control, "ShadowDash Cooldown", 1.5f); });
            shadowdashstate.AddAction(new Tk2dPlayAnimationWithEvents { animationCompleteEvent = finishedevent, gameObject = ownerdefault, clipName = "Shadow DashGV" });
            shadowdashstate.AddAction(new GetScale { gameObject = ownerdefault, xScale = xscale, everyFrame = false, yScale = 0f, zScale = 0f, space = 0, vector = new Vector3(0, 0) });
            shadowdashstate.AddAction(new FloatOperator { float1 = dashspeed, float2 = xscale, operation = (FloatOperator.Operation)2, storeResult = dashspeedvelo, everyFrame = false });
            shadowdashstate.AddAction(new SetVelocity2d { gameObject = ownerdefault, x = dashspeedvelo, everyFrame = false, vector = new Vector2(0, 0), y = 0 });
            shadowdashstate.AddAction(new Wait { finishEvent = finishedevent, realTime = false, time = dashtime });

            FsmState fireballdircheckstate = control.CreateState("Fireball Dir Check");
            fireballdircheckstate.AddAction(new BoolTest { boolVariable = fireballcooldown, everyFrame = false, isTrue = cancelevent });
            fireballdircheckstate.AddAction(new GetScale { gameObject = ownerdefault, xScale = xscale, everyFrame = false, yScale = 0f, zScale = 0f, space = 0, vector = new Vector3(0, 0) });
            fireballdircheckstate.AddAction(new FloatTestToBool { float1 = xscale, float2 = 0f, greaterThanBool = facingright, everyFrame = false, equalBool = false, lessThanBool = false, tolerance = 0f });
            fireballdircheckstate.AddAction(new CheckTargetDirection { gameObject = ownerdefault, target = herogameobject, rightBool = false, leftBool = heroisright, everyFrame = true, aboveBool = false, aboveEvent = blankevent, belowBool = false, belowEvent = blankevent, leftEvent = blankevent, rightEvent = blankevent });
            fireballdircheckstate.AddAction(new BoolTestMulti { boolVariables = isrightarray, boolStates = falsetruearray, trueEvent = turnevent, everyFrame = false, storeResult = false, });
            fireballdircheckstate.AddAction(new BoolTestMulti { boolVariables = isrightarray, boolStates = truefalsearray, trueEvent = turnevent, falseEvent = finishedevent, everyFrame = false, storeResult = false });

            FsmState fireballflipstate = control.CreateState("Fireball Flip");
            fireballflipstate.AddAction(new FlipScale { gameObject = ownerdefault, flipHorizontally = true, flipVertically = false, everyFrame = false, lateUpdate = false });

            string fireballname = "Fireball1 CastGV";
            if (Kindred_Spirit.DreamToggle)
            {
                fireballname = "Fireball2 CastGV";
            }

            FsmState fireballanticstate = control.CreateState("Fireball Antic");
            fireballanticstate.AddMethod(() => { Vessel.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic; });
            fireballanticstate.AddAction(new Tk2dPlayAnimationWithEvents { gameObject = ownerdefault, animationCompleteEvent = finishedevent, clipName = "Scream StartGV" });

            FsmState fireballstate = control.CreateState("Fireball");
            fireballstate.AddAction(new BoolTest { boolVariable = fireballcooldown, everyFrame = false, isTrue = cancelevent });
            fireballstate.AddMethod(() => { fireballattackobject.Value = Instantiate(fireballobject.Value); FireballInit(control); fireballattackobject.Value.gameObject.transform.position = Vessel.transform.position; });
            fireballstate.AddAction(new Tk2dPlayAnimationWithEvents { gameObject = ownerdefault, animationCompleteEvent = finishedevent, clipName = fireballname});

            FsmState fireballrecoilstate = control.CreateState("Fireball Recoil");
            fireballrecoilstate.AddMethod(() => { StartCoroutine(SpellCD(control, "Fireball Cooldown", fireballcd)); });
            fireballrecoilstate.AddAction(new Tk2dWatchAnimationEvents { animationCompleteEvent = finishedevent, gameObject = ownerdefault });
            fireballrecoilstate.AddAction(new FloatOperator { float1 = -1, float2 = xscale, operation = (FloatOperator.Operation)2, storeResult = fireballrecoilvelo, everyFrame = false });
            fireballrecoilstate.AddAction(new SetVelocity2d { gameObject = ownerdefault, x = fireballrecoilvelo, everyFrame = false, vector = new Vector2(0, 0), y = 0 });

            FsmState fireballpoststate = control.CreateState("Fireball Post");
            fireballpoststate.AddMethod(() => { Vessel.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Dynamic; });
            fireballpoststate.AddAction(new CheckCollisionSide { bottomHitEvent = finishedevent, ignoreTriggers = false, otherLayer = false, otherLayerNumber = 0, topHit = false, rightHit = false, bottomHit = false, leftHit = false });
            fireballpoststate.AddAction(new CheckCollisionSideEnter { bottomHitEvent = finishedevent, ignoreTriggers = false, otherLayer = false, otherLayerNumber = 0, topHit = false, rightHit = false, bottomHit = false, leftHit = false });
            fireballpoststate.AddAction(new Wait { finishEvent = fallevent, time = 0.1f, realTime = false });

            FsmState stunstate = control.CreateState("Stun");
            stunstate.AddMethod(() => { Vessel.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Dynamic; });
            stunstate.AddAction(new SetVelocity2d { gameObject = ownerdefault, x = 0, everyFrame = true, vector = new Vector2(0, 0), y = 0 });
            stunstate.AddAction(new SetFsmBool { gameObject = cameraparent, fsmName = "CameraShake", variableName = "RumblingMed", setValue = false, everyFrame = false });
            stunstate.AddAction(new SpawnObjectFromGlobalPool { gameObject = stungameobject, spawnPoint = control.gameObject, position = new Vector3(0, 0), rotation = new Vector3(0, 0), storeObject =  stunobjectstore});
            stunstate.AddAction(new Tk2dPlayAnimation { gameObject = ownerdefault, animLibName = "", clipName = "StunGV" });
            stunstate.AddAction(new Wait { finishEvent = endstunevent, realTime = false, time = 3f });

            FsmState stunrisestate = control.CreateState("Stun Rise");
            stunrisestate.AddAction(new Tk2dPlayAnimationWithEvents { gameObject = ownerdefault, animationCompleteEvent = finishedevent, clipName = "StunRiseGV" });

            FsmState introstate = control.CreateState("Intro");
            introstate.AddMethod(() => { MakeCorpse(); Vessel.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic; Vessel.GetComponent<BoxCollider2D>().enabled = false; Vessel.GetComponent<MeshRenderer>().enabled = false; introJump = true; });
            introstate.AddAction(new Wait { finishEvent = endintroevent, time = 3f, realTime = false });

            FsmState intro2state = control.CreateState("Intro 2");
            intro2state.AddMethod(() => { Vessel.GetComponent<BoxCollider2D>().enabled = true; Vessel.GetComponent<MeshRenderer>().enabled = true; corpse.SetActive(false); GrassEffect(); });
            intro2state.AddAction(new FlipScale { gameObject = ownerdefault, flipHorizontally = true, flipVertically = false, everyFrame = false, lateUpdate = false });
            intro2state.AddAction(new GetScale { gameObject = ownerdefault, xScale = xscale, everyFrame = false, yScale = 0f, zScale = 0f, space = 0, vector = new Vector3(0, 0) });
            intro2state.AddAction(new FloatOperator { float1 = runspeed, float2 = xscale, operation = (FloatOperator.Operation)2, storeResult = runspeedvelo, everyFrame = false });
            intro2state.AddAction(new SetVelocity2d { gameObject = ownerdefault, x = runspeedvelo, everyFrame = false, vector = new Vector2(0, 0), y = jumpspeed });
            intro2state.AddAction(new FlipScale { gameObject = ownerdefault, flipHorizontally = true, flipVertically = false, everyFrame = false, lateUpdate = false });
            intro2state.AddAction(new Tk2dPlayAnimationWithEvents { clipName = "IntroJumpGV", gameObject = ownerdefault });
            intro2state.AddAction(new Wait { finishEvent = endintroevent, realTime = false, time = 0.35f });


            FsmState kindredintrostate = control.CreateState("Kindred Intro");
            kindredintrostate.AddMethod(() => { MakeBench(); Vessel.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic; Vessel.transform.position += new Vector3(0, 0.4f); Vessel.GetComponent<BoxCollider2D>().enabled = false; });
            kindredintrostate.AddAction(new Tk2dPlayAnimationWithEvents { clipName = "Sitting AsleepGV", gameObject = ownerdefault });
            kindredintrostate.AddAction(new Wait { finishEvent = endintroevent, time = 3f, realTime = false });

            FsmState kindredintro2state = control.CreateState("Kindred Intro 2");
            kindredintro2state.AddAction(new Tk2dPlayAnimationWithEvents { clipName = "Wake To SitGV", gameObject = ownerdefault, animationCompleteEvent = endintroevent });

            FsmState kindredintro3state = control.CreateState("Kindred Intro 3");
            kindredintro3state.AddMethod(() => { Vessel.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Dynamic; });
            kindredintro3state.AddAction(new Tk2dPlayAnimationWithEvents { clipName = "Get OffGV", gameObject = ownerdefault, animationCompleteEvent = endintroevent });

            FsmState fallcheckstate = control.CreateState("Fall Check Intro");
            fallcheckstate.AddAction(new BoolTest { boolVariable = Kindred_Spirit.DreamToggle, everyFrame = false, isFalse = fallevent, isTrue = finishedevent });

            FsmState titlestate = control.CreateState("Title?");
            titlestate.AddAction(new GGCheckIfBossScene { regularSceneEvent = finishedevent });
            titlestate.AddMethod(() =>{ titlefsm.FsmVariables.GetFsmBool("Visited").Value = true; titlefsm.FsmVariables.GetFsmString("Area Event").Value = "FALLEN_VESSEL"; titlefsm.FsmVariables.GetFsmBool("Display Right").Value = true; titlefsm.gameObject.SetActive(true); });
            
            FsmState musicstate = control.CreateState("Music");
            musicstate.AddAction(new GGCheckIfBossSequence { trueEvent = finishedevent });
            musicstate.AddAction(new TransitionToAudioSnapshot { snapshot = snapshot, transitionTime = 0.5f });
            musicstate.AddAction(new GGCheckIfBossScene { regularSceneEvent = finishedevent });
            musicstate.AddAction(new ApplyMusicCue { musicCue = musiccue, delayTime = 0, transitionTime = 0 });

            FsmState deathstate = control.CreateState("Death");
            deathstate.AddMethod(() => { Vessel.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic; Vessel.Child("Hero Death(Clone)").SetActive(true); Vessel.GetComponent<MeshRenderer>().enabled = false; PlayOneShot(hurtsound); PlayOneShot(deathsound); });
            deathstate.AddAction(new SetVelocity2d { gameObject = ownerdefault, x = 0, everyFrame = true, vector = new Vector2(0, 0), y = 0 });
            deathstate.AddAction(new Tk2dPlayAnimation { animLibName = "", clipName = "DeathGV", gameObject = ownerdefault });
            deathstate.AddAction(new Wait { finishEvent = returnevent, realTime = false, time = 2f });

            FsmState returnstate = control.CreateState("Return");
            returnstate.AddMethod(() => { BossSceneController.Instance.bossesDeadWaitTime = 5; BossSceneController.Instance.EndBossScene(); Vessel.Child("Hero Death(Clone)").SetActive(false); furyeffect.Stop(); CreateHead(); });

            FsmState focusstate = control.CreateState("Focus Begin");
            focusstate.AddMethod(() => { if (!infury) { control.SendEvent("CANCEL"); } else { FocusBegin(); } });
            focusstate.AddAction(new Tk2dPlayAnimation { animLibName = "", clipName = "FocusGV", gameObject = ownerdefault });
            focusstate.AddAction(new Wait { finishEvent = finishedevent, realTime = false, time = 2.5f });

            FsmState focusgetstate = control.CreateState("Focus Get");
            focusgetstate.AddMethod(FocusGet);
            focusgetstate.AddAction(new Tk2dPlayAnimationWithEvents { clipName = "Focus Get OnceGV", gameObject = ownerdefault, animationCompleteEvent = finishedevent }); ;
            focusgetstate.AddMethod(() => { Destroy(furyeffect); infury = false; SetDamage(1); hp.hp += 150; });

            FsmState focusendstate = control.CreateState("Focus End");
            focusendstate.AddMethod(FocusEnd);
            focusendstate.AddAction(new Tk2dPlayAnimationWithEvents { clipName = "Focus EndGV", gameObject = ownerdefault, animationCompleteEvent = finishedevent });

            FsmState approachscreamcheckstate = control.CreateState("Approach Scream Check");
            approachscreamcheckstate.AddAction(new SendRandomEvent { events = approachscreamevents, delay = 0f, weights = approachscreamweights });

            FsmState prescreamstate = control.CreateState("PreScream");
            prescreamstate.AddMethod(() => { Vessel.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic; });
            prescreamstate.AddAction(new SetVelocity2d { gameObject = ownerdefault, x = 0, everyFrame = true, vector = new Vector2(0, 0), y = 0 });
            prescreamstate.AddAction(new Wait { time = 0.08f, finishEvent = finishedevent, realTime = false });

            FsmState screamstartstate = control.CreateState("Scream Start");
            screamstartstate.AddAction(new SetVelocity2d { gameObject = ownerdefault, x = 0, everyFrame = true, vector = new Vector2(0, 0), y = 0 });
            screamstartstate.AddAction(new Tk2dPlayAnimationWithEvents { clipName = "Scream StartGV", gameObject = ownerdefault, animationCompleteEvent = animendevent });
            screamstartstate.AddMethod(() => { PlayOneShot(screamsound); StartCoroutine(SpellCD(control, "Scream Cooldown", screamcd)); Vessel.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic; });

            FsmState screamstate = control.CreateState("Scream");
            screamstate.AddMethod(() => { Scream(); });
            screamstate.AddAction(new SetVelocity2d { gameObject = ownerdefault, x = 0, everyFrame = true, vector = new Vector2(0, 0), y = 0 });
            screamstate.AddAction(new Tk2dPlayAnimationWithEvents { clipName = "Scream 2GV", gameObject = ownerdefault, animationCompleteEvent = animendevent });
            screamstate.AddAction(new Wait { finishEvent = animendevent, realTime = false, time = 0.3f });

            FsmState screamendstate = control.CreateState("Scream End");
            screamendstate.AddAction(new Tk2dPlayAnimationWithEvents { clipName = "Scream End 2GV", gameObject = ownerdefault, animationCompleteEvent = animendevent });
            screamendstate.AddAction(new SetVelocity2d { gameObject = ownerdefault, x = 0, everyFrame = true, vector = new Vector2(0, 0), y = 0 });

            FsmState quakestartstate = control.CreateState("Quake Start");
            quakestartstate.AddAction(new SetVelocity2d { gameObject = ownerdefault, x = 0, everyFrame = true, vector = new Vector2(0, 0), y = 0 });
            quakestartstate.AddAction(new Tk2dPlayAnimationWithEvents { clipName = "Quake AnticGV", gameObject = ownerdefault, animationCompleteEvent = animendevent });
            quakestartstate.AddMethod(() => { StartCoroutine(SpellCD(control, "Quake Cooldown", quakecd)); Vessel.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic; QuakeBegin(); PlayOneShot(quakepreparesound); });

            FsmState quakefallstate = control.CreateState("Quake Fall");
            quakefallstate.AddMethod(() => { QuakeFall(); Vessel.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Dynamic; PlayOneShot(quakefallsound); });
            quakefallstate.AddAction(new SetVelocity2d { gameObject = ownerdefault, x = 0, everyFrame = true, vector = new Vector2(0, 0), y = -50 });
            quakefallstate.AddAction(new Tk2dPlayAnimationWithEvents { clipName = "Quake Fall 2", gameObject = ownerdefault });
            quakefallstate.AddAction(new CheckCollisionSide { bottomHitEvent = fallevent, ignoreTriggers = false, otherLayer = false, otherLayerNumber = 0, topHit = false, rightHit = false, bottomHit = false, leftHit = false });
            quakefallstate.AddAction(new CheckCollisionSideEnter { bottomHitEvent = fallevent, ignoreTriggers = false, otherLayer = false, otherLayerNumber = 0, topHit = false, rightHit = false, bottomHit = false, leftHit = false });
            quakefallstate.AddAction(new GetVelocity2d { everyFrame = true, gameObject = ownerdefault, y = yspeed, vector = new Vector2(0, 0), x = 0, space = Space.World });
            quakefallstate.AddAction(new FloatCompare { float1 = yspeed, float2 = 0, equal = fallevent, everyFrame = true, tolerance = 0.1f });

            FsmState quakelandstate = control.CreateState("Quake Land");
            quakelandstate.AddMethod(() => { QuakeLand(); PlayOneShot(quakelandsound); });
            quakelandstate.AddAction(new SetVelocity2d { gameObject = ownerdefault, x = 0, everyFrame = true, vector = new Vector2(0, 0), y = 0 });
            quakelandstate.AddAction(new Tk2dPlayAnimationWithEvents { clipName = "Quake Land 2", gameObject = ownerdefault, animationTriggerEvent = animendevent });
            quakelandstate.AddAction(new Wait { finishEvent = animendevent, realTime = false, time = 0.75f });

            FsmState quakemegastate = control.CreateState("Quake Mega");
            quakemegastate.AddMethod(QuakeMega);
            quakemegastate.AddAction(new Tk2dWatchAnimationEvents { animationCompleteEvent = animendevent, gameObject = ownerdefault });            

            FsmState beatdownstate = control.CreateState("Beatdown");
            beatdownstate.AddAction(new ApplyMusicCue { delayTime = 0f, transitionTime = 0f, musicCue = deathmusic });
            beatdownstate.AddAction(new SetVelocity2d { gameObject = ownerdefault, x = 0, everyFrame = true, vector = new Vector2(0, 0), y = 0 });
            beatdownstate.AddMethod(() => { stuncontrol.enabled = false; hp.hp = 300; Vessel.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Dynamic; StopSound("FootstepsRun"); });
            beatdownstate.AddAction(new Tk2dPlayAnimationWithEvents { clipName = "StunGV", gameObject = ownerdefault, animationTriggerEvent = animendevent });

            FsmState spiritdeathstate = control.CreateState("Spirit Death");
            spiritdeathstate.AddMethod(() => { Vessel.Child("Hero Death(Clone)").SetActive(true); Vessel.GetComponent<MeshRenderer>().enabled = false; PlayOneShot(hurtsound); PlayOneShot(deathsound); });
            spiritdeathstate.AddAction(new SetVelocity2d { gameObject = ownerdefault, x = 0, everyFrame = true, vector = new Vector2(0, 0), y = 0 });
            spiritdeathstate.AddAction(new Tk2dPlayAnimation { animLibName = "", clipName = "DeathGV", gameObject = ownerdefault });
            spiritdeathstate.AddAction(new Wait { finishEvent = returnevent, realTime = false, time = 2f });

            FsmState vesselremovestate = control.CreateState("Vessel Remove");
            vesselremovestate.AddMethod(() => { Vessel.Child("Hero Death(Clone)").SetActive(false); furyeffect.Stop(); CreateHead(); });
            vesselremovestate.AddAction(new Wait { finishEvent = returnevent, realTime = false, time = 1f });

            FsmState shadespawnstate = control.CreateState("Shade Spawn");
            shadespawnstate.AddMethod(() => { SpawnShade(); });

            //transitions
            idlestate.AddTransition(actionevent, "Action Choice");
            idlestate.AddTransition(turnevent, "Turn Anim");
            idlestate.AddTransition(slashevent, "Slash Choice");
            actionchoicestate.AddTransition(approachevent, "Approach Begin");
            actionchoicestate.AddTransition(jumpchaseevent, "Jump Chase");
            actionchoicestate.AddTransition(dashawayevent, "Dash Away");
            actionchoicestate.AddTransition(fireballevent, "Fireball Dir Check");
            actionchoicestate.AddTransition(focusevent, "Focus Begin");
            actionchoicestate.AddTransition(shadedashevent, "Shadow Dash");
            jumpstate.AddTransition(endjumpevent, "Double Jump Check");
            doublejumpcheckstate.AddTransition(doublejumpevent, "Double Jump");
            doublejumpcheckstate.AddTransition("CANCEL", "Fall");
            fallstate.AddTransition(finishedevent, "Land");
            fallstate.AddTransition(slashevent, "DownSlash");
            fallstate.AddTransition(fireballevent, "Fireball Dir Check");
            landstate.AddTransition(finishedevent, "Idle");
            turnanimstate.AddTransition(finishedevent, "Turn Flip");
            turnflipstate.AddTransition("FINISHED", "Idle");
            slashoption.AddTransition(slashevent, "Slash");
            slashoption.AddTransition(slashaltevent, "SlashAlt");
            slashoption.AddTransition(upslashevent, "UpSlash");
            slashstate.AddTransition(finishedevent, "Post Slash");
            altslashstate.AddTransition(finishedevent, "Post Slash");
            upslashstate.AddTransition(finishedevent, "Post Slash");
            postslashstate.AddTransition(finishedevent, "Idle");
            downslashstate.AddTransition(finishedevent, "Fall");
            slashdelaystate.AddTransition(finishedevent, "Slash Choice");
            approachbeginstate.AddTransition("FINISHED", "Approach");
            approachstate.AddTransition(turnevent, "Approach Turn");
            approachstate.AddTransition(finishedevent, "Idle");
            approachstate.AddTransition(slashevent, "Slash Delay");
            approachturnstate.AddTransition(finishedevent, "Approach Turn Flip");
            approachturnflipstate.AddTransition("FINISHED", "Approach");
            dashawaystate.AddTransition(finishedevent, "Idle");
            shadowdashstate.AddTransition(finishedevent, "Idle");
            fireballdircheckstate.AddTransition(turnevent, "Fireball Flip");
            fireballdircheckstate.AddTransition(finishedevent, "Fireball Antic");
            fireballdircheckstate.AddTransition(cancelevent, "Fireball Post");
            fireballflipstate.AddTransition("FINISHED", "Fireball Antic");
            fireballanticstate.AddTransition(finishedevent, "Fireball");
            fireballstate.AddTransition(finishedevent, "Fireball Recoil");
            fireballstate.AddTransition(cancelevent, "Fireball Post");
            fireballrecoilstate.AddTransition(finishedevent, "Fireball Post");
            fireballpoststate.AddTransition(finishedevent, "Idle");
            fireballpoststate.AddTransition(fallevent, "Fall");
            stunstate.AddTransition(endstunevent, "Stun Rise");
            stunstate.AddTransition("TOOKDAMAGE", "Stun Rise");
            stunrisestate.AddTransition(finishedevent, "Idle");
            introstate.AddTransition(endintroevent, "Intro 2");
            intro2state.AddTransition(endintroevent, "Title?");
            kindredintrostate.AddTransition(endintroevent, "Kindred Intro 2");
            kindredintro2state.AddTransition(endintroevent, "Kindred Intro 3");
            kindredintro3state.AddTransition(endintroevent, "Title?");
            titlestate.AddTransition("FINISHED", "Music");
            musicstate.AddTransition("FINISHED", "Fall Check Intro");
            fallcheckstate.AddTransition(finishedevent, "Idle");
            fallcheckstate.AddTransition(fallevent, "Fall");
            deathstate.AddTransition(returnevent, "Return");
            focusstate.AddTransition(finishedevent, "Focus Get");
            focusstate.AddTransition("CANCEL", "Idle");
            focusstate.AddTransition("TOOKDAMAGE", "Focus End");
            focusgetstate.AddTransition(finishedevent, "Focus End");
            focusendstate.AddTransition(finishedevent, "Idle");
            prescreamstate.AddTransition(finishedevent, "Scream Start");
            screamstartstate.AddTransition(animendevent, "Scream");
            screamstate.AddTransition(animendevent, "Scream End");
            screamendstate.AddTransition(animendevent, "Fall");
            approachscreamcheckstate.AddTransition(screamevent, "PreScream");
            approachscreamcheckstate.AddTransition(finishedevent, "Approach");
            quakestartstate.AddTransition(animendevent, "Quake Fall");
            quakefallstate.AddTransition(fallevent, "Quake Land");
            quakelandstate.AddTransition(animendevent, "Quake Mega");
            quakemegastate.AddTransition(animendevent, "Idle");
            doublejumpstate.AddTransition(endjumpevent, "Fall");
            beatdownstate.AddTransition("Die", "Spirit Death");
            spiritdeathstate.AddTransition(returnevent, "Vessel Remove");
            vesselremovestate.AddTransition(returnevent, "Shade Spawn");

            if (Kindred_Spirit.DreamToggle)
            {
                approachstate.AddTransition(screamevent, "Approach Scream Check");
                fallstate.AddTransition(slashaltevent, "Slash Choice");
                fallstate.AddTransition(screamevent, "PreScream");
                fallstate.AddTransition(quakeevent, "Quake Start");
            }


            //global transitions
            FsmTransition[] globaltrans = new FsmTransition[3];
            globaltrans[0] = new FsmTransition { FsmEvent = FsmEvent.GetFsmEvent("STUN") ?? new FsmEvent("STUN"), ToFsmState = control.Fsm.GetState("Stun") };
            globaltrans[1] = new FsmTransition { FsmEvent = FsmEvent.GetFsmEvent("DEATH") ?? new FsmEvent("DEATH"), ToFsmState = control.Fsm.GetState("Death") };          
            globaltrans[2] = new FsmTransition { FsmEvent = FsmEvent.GetFsmEvent("BEATDOWN") ?? new FsmEvent("BEATDOWN"), ToFsmState = control.Fsm.GetState("Beatdown") };

            control.Fsm.GlobalTransitions = globaltrans;

            StartCoroutine(BeginIntro(control));
        }

        private IEnumerator BeginIntro(PlayMakerFSM control)
        {
            yield return new WaitForFinishedEnteringScene();
            if (Kindred_Spirit.DreamToggle)
            {
                control.SetState("Kindred Intro");
            }
            else
            {
                control.SetState("Intro");
            }
        }

        private void CreateHead()
        {
            ResourceLoader.nosk.GetComponent<EnemyDeathEffects>().PreInstantiate();
            GameObject headclone = Instantiate(ResourceLoader.nosk.Child("Corpse Mimic Spider(Clone)").Child("Head"));
            Destroy(headclone.Child("Corpse Steam"));
            headclone.transform.position = Vessel.transform.position + new Vector3(0, -0.02f, -0.01f);
            float random = UnityEngine.Random.Range(-15, 15);
            headclone.transform.SetRotationZ(random);
            headclone.SetActive(true);

            //shade fx
            GameObject particles = Vessel.Child("Hero Death(Clone)").Child("Shade Particles");
            particles.transform.parent = null;
            particles.SetActive(false);
            particles.SetActive(true);

            //change sprite -- i know loading each time is a little inefficent, but its really just so small
            headclone.GetComponent<SpriteRenderer>().sprite = ResourceLoader.LoadSprite("Kindred_Spirit.Resources.Sprites.Head.png");
            headclone.transform.localScale = new Vector3(1.6f, 1.6f, 1);
            headclone.GetComponent<BoxCollider2D>().size = new Vector2(0.51f, 0.7f);
            headclone.Child("Terrain Box").transform.localScale = new Vector3(1, 0.7f, 1);

        }

        private void MakeCorpse()
        {
            corpse = new GameObject();
            SpriteRenderer render = corpse.AddComponent<SpriteRenderer>();
            render.sprite = ResourceLoader.LoadSprite("Kindred_Spirit.Resources.Sprites.Corpse.png");
            corpse.transform.position = vesselspawnpos + new Vector3(0, -0.8f, 0.05f);
            corpse.transform.localScale = new Vector3(-1.5f, 1.5f, 1f);

        }

        private void MakeBench()
        {
            GameObject bench = new GameObject();
            SpriteRenderer render = bench.AddComponent<SpriteRenderer>();
            render.sprite = ResourceLoader.LoadSprite("Kindred_Spirit.Resources.Sprites.bench.png");
            bench.transform.position = vesselspawnpos + new Vector3(0, -0.5f, 0.1f);
            bench.transform.localScale = new Vector3(2, 2, 1);

        }

        private void SpawnShade()
        {
            GameObject shade = Instantiate(ResourceLoader.shadeenemy);
            PlayMakerFSM control = shade.LocateMyFSM("Control");
            FsmState friendlystate = control.GetState("Friendly?");
            friendlystate.GetAction<SetDamageHeroAmount>(4).damageDealt = 2;
            friendlystate.GetAction<SetHP>(6).hp = 300;
            friendlystate.GetAction<SetBoolValue>(7).boolValue = false;

            control.GetState("Init").InsertMethod(1, () => { control.FsmVariables.GetFsmString("Number").Value = " 1"; });
            control.GetState("Chase").GetAction<RandomFloat>().min = 10f;
            control.GetState("Chase").GetAction<RandomFloat>().max = 10f;
            control.GetState("Disable Collider").InsertMethod(0, () => { BossSceneController.Instance.bossesDeadWaitTime = 3; BossSceneController.Instance.EndBossScene(); });

            shade.transform.position = Vessel.transform.position;
            shade.SetActive(true);
        }

        private IEnumerator SpellCD(PlayMakerFSM control, string name, float time)
        {            
            FsmBool spellcd = control.FsmVariables.GetFsmBool(name);

            if (!spellcd.Value)
            {
                spellcd.Value = true;
                yield return new WaitForSeconds(time);
                spellcd.Value = false;
            }
        }

        private void SetDamage(int damage)
        {
            if (Kindred_Spirit.DreamToggle)
            {
                damage += 1;
            }
            foreach (DamageHero dmg in Vessel.GetComponentsInChildren<DamageHero>())
            {
                dmg.damageDealt = damage;
            }
            Vessel.GetComponent<DamageHero>().damageDealt = damage;
        }

        private void TakeDamageEvent(On.HealthManager.orig_TakeDamage orig, HealthManager self, HitInstance hitInstance)
        {
            orig(self, hitInstance);
            if (self.name == Vessel.name)
            {
                PlayMakerFSM control = Vessel.LocateMyFSM("Control");
                if (control != null)
                {
                    control.SendEvent("TOOKDAMAGE");
                }
                PlayMakerFSM stuncontrol = Vessel.LocateMyFSM("Stun Control");
                if (control != null)
                {
                    stuncontrol.SendEvent("STUN DAMAGE");
                }

                int val = 250;
                if (Kindred_Spirit.DreamToggle)
                {
                    val = 550;
                }

                if (self.hp < val && !infury && !furyactivated)
                {
                    infury = true;
                    furyactivated = true;
                    ParticleSystem particles = HeroController.instance.gameObject.Child("Charm Effects").Child("Fury").GetComponent<ParticleSystem>();
                    if (furyeffect == null)
                    {
                        furyeffect = Instantiate(particles, Vessel.transform);
                    }
                    furyeffect.Play();
                    furybursteffect.SetActive(true);
                    PlayOneShot(hurtsound);
                    SetDamage(2);
                }
                if (Kindred_Spirit.DreamToggle && self.hp <= 300 && control.ActiveStateName != "Beatdown" && !died)
                {
                    died = true;
                    control.SendEvent("BEATDOWN");
                }
            }
        }


        //look i know it looks bad and inefficient but it works and i dont really care
        private void FocusBegin()
        {
            GameObject focuseffects = Vessel.Child("Focus Effects(Clone)");
            if (focuseffects)
            {
                GameObject lines = focuseffects.Child("Lines Anim");
                GameObject chargeaudio = focuseffects.Child("Charge Audio");

                lines.GetComponent<MeshRenderer>().enabled = true;
                lines.GetComponent<tk2dSpriteAnimator>().Play();
                chargeaudio.GetComponent<AudioSource>().Play();
            }
        }

        private void FocusEnd()
        {
            GameObject focuseffects = Vessel.Child("Focus Effects(Clone)");
            if (focuseffects)
            {
                GameObject lines = focuseffects.Child("Lines Anim");
                GameObject chargeaudio = focuseffects.Child("Charge Audio");

                lines.GetComponent<MeshRenderer>().enabled = false;
                lines.GetComponent<tk2dSpriteAnimator>().Stop();
                chargeaudio.GetComponent<AudioSource>().Stop();
            }
        }

        private void FocusGet()
        {
            GameObject focuseffects = Vessel.Child("Focus Effects(Clone)");
            if (focuseffects)
            {
                GameObject lines = focuseffects.Child("Lines Anim");
                GameObject dustl = focuseffects.Child("Dust L");
                GameObject dustr = focuseffects.Child("Dust R");
                GameObject heal = focuseffects.Child("Heal Anim");
                GameObject chargeaudio = focuseffects.Child("Charge Audio");
                FsmObject healaudio = HeroController.instance.spellControl.GetAction<AudioPlayerOneShotSingle>("Focus Heal", 3).audioClip;

                lines.GetComponent<MeshRenderer>().enabled = false;
                lines.GetComponent<tk2dSpriteAnimator>().Stop();
                chargeaudio.GetComponent<AudioSource>().Stop();

                heal.SetActive(true);
                dustl.GetComponent<ParticleSystem>().Emit(10);
                dustr.GetComponent<ParticleSystem>().Emit(10);

                Vessel.GetComponent<AudioSource>().PlayOneShot((AudioClip)healaudio.Value);
            }
        }

        private void AddParryFsm(GameObject obj)
        {
            PlayMakerFSM clashfsm = obj.AddComponent<PlayMakerFSM>();
            clashfsm.enabled = false;
            clashfsm.Fsm.States = clashfsm.Fsm.States.RemoveFirst(x => x.Name == "State 1").ToArray();
            clashfsm.FsmName = "Clash";

            FsmOwnerDefault ownerdefault = new FsmOwnerDefault();
            ownerdefault.GameObject = obj;
            ownerdefault.OwnerOption = OwnerDefaultOption.SpecifyGameObject;

            PlayMakerFSM templatefsm = ResourceLoader.hkprime.Child("Slashes").Child("Slash1").LocateMyFSM("FSM");

            FsmFloat[] lessthanarray = new FsmFloat[4];
            lessthanarray[0] = new FsmFloat(45f);
            lessthanarray[1] = new FsmFloat(135f);
            lessthanarray[2] = new FsmFloat(225f);
            lessthanarray[3] = new FsmFloat(360f);

            FsmOwnerDefault gamemanagerowner = templatefsm.GetAction<SendMessage>("Blocked Hit", 0).gameObject;
            FunctionCall freezefunction = templatefsm.GetAction<SendMessage>("Blocked Hit", 0).functionCall;
            FsmOwnerDefault heroownerdefault = templatefsm.GetAction<SendMessage>("Blocked Hit", 1).gameObject;
            FunctionCall nailparryfunction = templatefsm.GetAction<SendMessage>("Blocked Hit", 1).functionCall;
            FsmGameObject nailclasheffect = templatefsm.GetAction<SpawnObjectFromGlobalPool>("No Box Right", 1).gameObject;
            FsmGameObject herogameobject = templatefsm.GetAction<SpawnObjectFromGlobalPool>("No Box Right", 1).spawnPoint;
            FunctionCall nailparryrecoverfunction = templatefsm.GetAction<SendMessage>("NailParryRecover", 0).functionCall;
            FsmEventTarget cameraowner = templatefsm.GetAction<SendEventByName>("Blocked Hit", 4).eventTarget;
            AudioClip[] audioclips = templatefsm.GetAction<AudioPlayerOneShot>("Blocked Hit", 5).audioClips;
            FsmFloat[] weights = templatefsm.GetAction<AudioPlayerOneShot>("Blocked Hit", 5).weights;
            AudioSource audiosource = obj.transform.parent.parent.GetComponent<AudioSource>();
            FsmGameObject audioplayeractor = templatefsm.GetAction<AudioPlayerOneShot>("Blocked Hit", 5).audioPlayer;

            //testing
            FsmVar storeresultarray = templatefsm.GetAction<CallMethodProper>("No Box Right", 0).storeResult;
            FsmVar[] parameters = templatefsm.GetAction<CallMethodProper>("No Box Right", 0).parameters;
            FsmGameObject storeobjecteffect = templatefsm.GetAction<SpawnObjectFromGlobalPool>("No Box Right", 1).storeObject;

            //events
            FsmEvent finishedevent = clashfsm.CreateFsmEvent("FINISHED");
            FsmEvent takedamageevent = clashfsm.CreateFsmEvent("TAKE DAMAGE");
            FsmEvent upevent = clashfsm.CreateFsmEvent("UP");
            FsmEvent downevent = clashfsm.CreateFsmEvent("DOWN");
            FsmEvent leftevent = clashfsm.CreateFsmEvent("LEFT");
            FsmEvent rightevent = clashfsm.CreateFsmEvent("RIGHT");

            FsmEvent[] eventarray = new FsmEvent[4];
            eventarray[0] = rightevent;
            eventarray[1] = upevent;
            eventarray[2] = leftevent;
            eventarray[3] = downevent;

            //vars
            clashfsm.FsmVariables.GameObjectVariables = templatefsm.FsmVariables.GameObjectVariables;
            FsmGameObject selfobject = clashfsm.FsmVariables.GetFsmGameObject("Self");
            FsmGameObject slashobject = clashfsm.FsmVariables.GetFsmGameObject("Slash");
            FsmGameObject parentobject = clashfsm.FsmVariables.GetFsmGameObject("Parent");
            FsmGameObject colliderobject = clashfsm.FsmVariables.GetFsmGameObject("Collider");
            FsmFloat attackdirection = clashfsm.CreateFsmFloat("Attack Direction", 0);

            //states
            FsmState initatestate = clashfsm.CreateState("Initiate");
            initatestate.AddAction(new GetOwner { storeGameObject = selfobject });
            initatestate.AddAction(new GetParent { gameObject = ownerdefault, storeResult = parentobject });

            FsmState detectingstate = clashfsm.CreateState("Detecting");
            detectingstate.AddAction(new Trigger2dEventLayer { trigger = 0, collideLayer = 16, collideTag = "", sendEvent = takedamageevent, storeCollider = colliderobject });

            FsmState blockedhitstate = clashfsm.CreateState("Blocked Hit");
            blockedhitstate.AddAction(new SendMessage { gameObject = gamemanagerowner, delivery = 0, options = (SendMessageOptions)1, functionCall = freezefunction });
            blockedhitstate.AddAction(new SendMessage { gameObject = heroownerdefault, delivery = 0, options = (SendMessageOptions)1, functionCall = nailparryfunction });
            blockedhitstate.AddAction(new SendEventByName { eventTarget = cameraowner, sendEvent = "EnemyKillShake", delay = 0f, everyFrame = false });
            blockedhitstate.AddMethod(() => { audiosource.pitch = UnityEngine.Random.Range(0.85f, 1.15f); audiosource.PlayOneShot(audioclips[0]); });
            blockedhitstate.AddMethod(() => { slashobject.Value = colliderobject.Value.transform.parent.gameObject; });
            blockedhitstate.AddMethod(() => { attackdirection.Value = slashobject.Value.LocateMyFSM("damages_enemy").FsmVariables.GetFsmFloat("direction").Value; });
            blockedhitstate.AddAction(new FloatSwitch{ floatVariable = attackdirection, lessThan = lessthanarray, sendEvent = eventarray, everyFrame = false });

            FsmState rightstate = clashfsm.CreateState("No Box Right");
            rightstate.AddAction(new CallMethodProper { gameObject = heroownerdefault, behaviour = "HeroController", methodName = "RecoilLeft", storeResult = storeresultarray, parameters = parameters });
            rightstate.AddAction(new SpawnObjectFromGlobalPool { gameObject = nailclasheffect, spawnPoint = herogameobject, position = new Vector3(1.5f, 0f), rotation = new Vector3(0, 0), storeObject = storeobjecteffect });

            FsmState leftstate = clashfsm.CreateState("No Box Left");
            leftstate.AddAction(new CallMethodProper { gameObject = heroownerdefault, behaviour = "HeroController", methodName = "RecoilRight", storeResult = storeresultarray, parameters = parameters });
            leftstate.AddAction(new SpawnObjectFromGlobalPool { gameObject = nailclasheffect, spawnPoint = herogameobject, position = new Vector3(-1.5f, 0f), rotation = new Vector3(0, 0), storeObject = storeobjecteffect });

            FsmState upstate = clashfsm.CreateState("No Box Up");
            upstate.AddAction(new CallMethodProper { gameObject = heroownerdefault, behaviour = "HeroController", methodName = "RecoilDown", storeResult = storeresultarray, parameters = parameters });
            upstate.AddAction(new SpawnObjectFromGlobalPool { gameObject = nailclasheffect, spawnPoint = herogameobject, position = new Vector3(0f, 1.5f), rotation = new Vector3(0, 0), storeObject = storeobjecteffect });

            FsmState downstate = clashfsm.CreateState("No Box Down");
            downstate.AddAction(new CallMethodProper { gameObject = heroownerdefault, behaviour = "HeroController", methodName = "Bounce", storeResult = storeresultarray, parameters = parameters });
            downstate.AddAction(new SpawnObjectFromGlobalPool { gameObject = nailclasheffect, spawnPoint = herogameobject, position = new Vector3(0f, -1.5f), rotation = new Vector3(0, 0), storeObject = storeobjecteffect });

            FsmState pauseframestate = clashfsm.CreateState("Pause Frame");
            pauseframestate.AddAction(new Wait { time = 0.1f, finishEvent = finishedevent, realTime = false });

            FsmState nailparryrecoverstate = clashfsm.CreateState("NailParryRecover");
            nailparryrecoverstate.AddAction(new SendMessage { gameObject = heroownerdefault, delivery = 0, options = (SendMessageOptions)1, functionCall = nailparryrecoverfunction });
            nailparryrecoverstate.AddAction(new NextFrameEvent { sendEvent = finishedevent });


            initatestate.AddTransition("FINISHED", "Detecting");
            detectingstate.AddTransition(takedamageevent, "Blocked Hit");
            blockedhitstate.AddTransition(rightevent, "No Box Right");
            blockedhitstate.AddTransition(leftevent, "No Box Left");
            blockedhitstate.AddTransition(upevent, "No Box Up");
            blockedhitstate.AddTransition(downevent, "No Box Down");
            rightstate.AddTransition("FINISHED", "Pause Frame");
            leftstate.AddTransition("FINISHED", "Pause Frame");
            upstate.AddTransition("FINISHED", "Pause Frame");
            downstate.AddTransition("FINISHED", "Pause Frame");
            pauseframestate.AddTransition("FINISHED", "NailParryRecover");
            nailparryrecoverstate.AddTransition("FINISHED", "Detecting");

            clashfsm.enabled = true;
            clashfsm.SetState("Initiate");

        }

        private void PlaySound(string name)
        {
            GameObject soundobject = Vessel.Child("Sounds(Clone)").Child(name);
            if (soundobject != null)
            {
                if (soundobject.name == "FootstepsRun")
                {
                    soundobject.GetComponent<AudioSource>().loop = true;
                }
                soundobject.GetComponent<AudioSource>().Play();
            }
        }

        private void PlayOneShot(AudioClip clip)
        {
            AudioSource source = Vessel.GetComponent<AudioSource>();
            if (source)
            {
                source.PlayOneShot(clip);
            }
        }

        private void StopSound(string name)
        {
            GameObject soundobject = Vessel.Child("Sounds(Clone)").Child(name);
            if (soundobject != null)
            {
                soundobject.GetComponent<AudioSource>().Stop();
            }
        }

        private void PlayDash(GameObject obj)
        {
            GameObject dasheffect = obj.Child("Effects(Clone)").Child("Dash Burst");
            dasheffect.transform.rotation = new Quaternion(0, 0, 0, 0);
            dasheffect.SetActive(true);
            dasheffect.GetComponent<PlayMakerFSM>().SendEvent("PLAY");
        }

        private void PlayShadowDash()
        {
            GameObject effects = Vessel.Child("Effects(Clone)");
            GameObject shadowring = effects.Child("Shadow Ring");
            GameObject shadowburst = effects.Child("Shadow Burst");
            GameObject shadowrecharge = effects.Child("Shadow Recharge");
            GameObject blobs = effects.Child("Shadow Dash Blobs");

            if (shadowburst && shadowring && shadowrecharge && blobs)
            {
                shadowburst.transform.rotation = new Quaternion(0, 0, 0, 0);
                shadowring.SetActive(true);
                shadowburst.SetActive(true);
                shadowrecharge.SetActive(true);
                blobs.GetComponent<ParticleSystem>().Emit(15);
            }
        }

        private void GrassEffect()
        {
            GameObject grass = Instantiate(ResourceLoader.megamoss.Child("Grass Shake"));
            grass.transform.parent = null;
            grass.transform.position = Vessel.transform.position;
            grass.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
            grass.GetComponent<ParticleSystem>().Emit(20);

            AudioClip clip = (AudioClip)ResourceLoader.megamoss.LocateMyFSM("Mossy Control").GetAction<AudioPlayerOneShotSingle>("Emerge", 1).audioClip.Value;
            PlayOneShot(clip);
        }

        private void CreateRange(GameObject clone, string name, Vector2 size, Vector2 offset)
        {
            GameObject slashrange = new GameObject(name);
            slashrange.transform.parent = clone.transform;
            slashrange.transform.localPosition = new Vector3(0, 0);
            slashrange.transform.localScale = new Vector3(1, 1, 1);
            BoxCollider2D collider = slashrange.AddComponent<BoxCollider2D>();
            collider.size = size;
            collider.offset = offset;
            collider.isTrigger = true;

            slashrange.AddComponent<RangeCheck>();
        }

        private void FireballInit(PlayMakerFSM control)
        {
            //hell
            GameObject fb = control.FsmVariables.GetFsmGameObject("Fireball Object").Value;

            PlayMakerFSM fireballcast = fb.LocateMyFSM("Fireball Cast");
            fireballcast.GetState("Recycle").RemoveAction(0);
            fireballcast.GetState("Recycle").AddMethod(() => { Destroy(fb); });

            FsmGameObject fireballstore = fireballcast.CreateFsmGameObject("FireballObj");

            int indexR = Kindred_Spirit.DreamToggle ? 4 : 7;
            int indexL = 4;

            FsmGameObject fireballorig = fireballcast.GetAction<SpawnObjectFromGlobalPool>("Cast Right", indexR).gameObject;
            
            //replace with instantiate
            fireballcast.GetState("Cast Right").RemoveAction(indexR);
            fireballcast.GetState("Cast Left").RemoveAction(indexL);

            fireballcast.GetState("Cast Right").InsertMethod(indexR, () => { fireballstore.Value = Instantiate(fireballorig.Value); fireballstore.Value.gameObject.transform.position = Vessel.transform.position; });
            fireballcast.GetState("Cast Left").InsertMethod(indexL, () => { fireballstore.Value = Instantiate(fireballorig.Value); fireballstore.Value.gameObject.transform.position = Vessel.transform.position; });


            //fixxerupper
            fireballcast.GetState("Wait").AddMethod(() =>
            { 
                fireballstore.Value.LocateMyFSM("damages_enemy").enabled = false;
                //dmg
                DamageHero dmg = fireballstore.Value.AddComponent<DamageHero>();

                //scale
                fireballstore.Value.gameObject.transform.localScale = new Vector3(1, 1, 1);
                if (Kindred_Spirit.DreamToggle)
                {
                    fireballstore.Value.gameObject.transform.localScale = new Vector3(1.8f, 1.8f, 1);
                }

                PlayMakerFSM fireballcontrol = fireballstore.Value.LocateMyFSM("Fireball Control");
                fireballcast.FsmName = "FireballControl";
                //remove wall transition
                if (!Kindred_Spirit.DreamToggle)
                {
                    fireballcontrol.GetState("Idle").Transitions[1].FsmEvent = new FsmEvent("");
                    //fix size for non-dream
                    fireballcontrol.GetState("Set Damage").RemoveAction(6);
                }

                //velocity stuff -- I KNOW ITS BAD ANDA HORRIBLE I DONT CARE I DID NOT PLAN FOR THIS AT ALL
                if (Kindred_Spirit.DreamToggle)
                {
                    FsmOwnerDefault ownerdefault = fireballcontrol.GetState("R").GetAction<FindChild>(0).gameObject;
                    int valx = -45;
                    float val = valx * Vessel.gameObject.transform.GetScaleX();
                    float xscale = Vessel.gameObject.transform.GetScaleX() * -1.8f;
                    fireballcontrol.GetState("Idle").InsertAction(0, new SetScale { gameObject = ownerdefault, everyFrame = true, x = xscale, y = 1.8f, z = 1.8f, vector = new Vector3(0, 0) });
                    fireballcontrol.GetState("Idle").InsertAction(0, new SetVelocity2d { gameObject = ownerdefault, everyFrame = true, y = 0, x = val, vector = new Vector2(0, 0) });
                } else
                {
                    FsmOwnerDefault ownerdefault = fireballcontrol.GetState("Init").GetAction<GetVelocity2d>(0).gameObject;
                    int valx = -40;
                    float val = valx * Vessel.gameObject.transform.GetScaleX();
                    fb.transform.SetScaleX(Vessel.gameObject.transform.GetScaleX());
                    fb.transform.SetScaleY(1); 
                    fireballcontrol.GetState("Init").InsertAction(0, new SetVelocity2d { gameObject = ownerdefault, everyFrame = false, y = 0, x = val, vector = new Vector2(0, 0) });
                }

                //replace recycle
                if (Kindred_Spirit.DreamToggle)
                {
                    fireballcontrol.GetState("Recycle").RemoveAction(0);
                    fireballcontrol.GetState("Recycle").AddMethod(() => { Destroy(fireballstore.Value); });
                    
                } else
                {
                    fireballcontrol.GetState("Diss R").RemoveAction(0);
                    fireballcontrol.GetState("Break R").RemoveAction(0);

                    fireballcontrol.GetState("Diss R").AddMethod(() => { Destroy(fireballstore.Value); });
                    fireballcontrol.GetState("Break R").AddMethod(() => { Destroy(fireballstore.Value); });
                }
            });

            fireballcast.GetState("Cast Left").RemoveTransition("FLUKE");
            fireballcast.GetState("Cast Right").RemoveTransition("FLUKE");


        }

        //dont care didnt ask

        private void RemoveFsms(GameObject obj)
        {
            foreach (PlayMakerFSM fsm in obj.GetComponents<PlayMakerFSM>())
            {
                Destroy(fsm);
            }
            foreach (PlayMakerFixedUpdate fsm in obj.GetComponents<PlayMakerFixedUpdate>())
            {
                Destroy(fsm);
            }
            foreach (PlayMakerCollisionEnter2D fsm in obj.GetComponents<PlayMakerCollisionEnter2D>())
            {
                Destroy(fsm);
            }
            foreach (PlayMakerUnity2DProxy fsm in obj.GetComponents<PlayMakerUnity2DProxy>())
            {
                Destroy(fsm);
            }
        }

        private void RemoveFsmsFromChildren(GameObject obj)
        {
            foreach (PlayMakerFSM fsm in obj.GetComponentsInChildren<PlayMakerFSM>())
            {
                Destroy(fsm);
            }
            foreach (PlayMakerFixedUpdate fsm in obj.GetComponentsInChildren<PlayMakerFixedUpdate>())
            {
                Destroy(fsm);
            }
            foreach (PlayMakerCollisionEnter2D fsm in obj.GetComponentsInChildren<PlayMakerCollisionEnter2D>())
            {
                Destroy(fsm);
            }
            foreach (PlayMakerUnity2DProxy fsm in obj.GetComponentsInChildren<PlayMakerUnity2DProxy>())
            {
                Destroy(fsm);
            }
        } 

        private void PurgeFsms(GameObject obj)
        {
            RemoveFsms(obj);
            RemoveFsmsFromChildren(obj);
        }
    }
}
