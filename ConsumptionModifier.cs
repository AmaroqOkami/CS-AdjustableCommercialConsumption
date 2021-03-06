using System.Reflection;
using System.Collections.Generic;
using ICities;
using ColossalFramework;
using System.Collections;
using UnityEngine;

namespace AdjustableCommercialConsumption
{
    public class GoodsMonitor : ThreadingExtensionBase
    {
        private readonly BuildingManager buildingManager;
        private readonly SimulationManager simulationManager;
        private readonly TransferManager.TransferReason[] industryGoods =
            { TransferManager.TransferReason.Oil,
              TransferManager.TransferReason.Coal,
              TransferManager.TransferReason.Grain,
              TransferManager.TransferReason.Logs,
              TransferManager.TransferReason.Petrol,
              TransferManager.TransferReason.Ore,
              TransferManager.TransferReason.Food,
              TransferManager.TransferReason.Lumber };

        private Dictionary<ushort, int> comGoodsCount = new Dictionary<ushort, int>();
        private Dictionary<ushort, int> indGoodsCount = new Dictionary<ushort, int>();

        public static bool startDelayed = false;
        private int refTransferAmt = 0;

        public static int GoodsReplenishAmount = 0;
        public static int GoodsOldTotalAmount = 0;
        public static int GoodsTotalAmount = 0;
        public static int GoodsReadAmount = 0;
        public static int GoodsNewAmount = 0;

        public static int indGoodsReplenishAmount = 0;
        public static int indGoodsOldTotalAmount = 0;
        public static int indGoodsTotalAmount = 0;
        public static int indGoodsReadAmount = 0;
        public static int indGoodsNewAmount = 0;

        public static int highComBuildingCount = 0;


        public GoodsMonitor()
        {
            buildingManager = Singleton<BuildingManager>.instance;
            simulationManager = Singleton<SimulationManager>.instance;
        }

        public override void OnAfterSimulationTick()
        {
            base.OnAfterSimulationTick();

            if (startDelayed)
            { GoodsCheck(); }
        }

        public void GoodsCheck()
        {

            if (ACC_Options.Instance.pauseRefillEnable || !simulationManager.SimulationPaused)
            {

                Building building;
                BuildingInfo info;
                BuildingAI ai;

                for (var buildingId = (ushort)(simulationManager.m_currentTickIndex % 1000);
                    buildingId < buildingManager.m_buildings.m_buffer.Length;
                    buildingId += 1000)
                {
                    building = buildingManager.m_buildings.m_buffer[buildingId];

                    info = building.Info;
                    if (info == null) continue;

                    ai = info.GetAI() as BuildingAI;
                    if (ai == null) continue;

                    if (ai is CommercialBuildingAI)
                    {
                        //Resupply building based on delta between current tick and last tick.
                        int addAmount = 0;
                        int amount = building.m_customBuffer1;

                        if (!ACC_Options.Instance.refillBuildingsEnable && comGoodsCount.TryGetValue(buildingId, out int oldAmount))
                        {
                            //Check if the commercial building is high-density, and adjust accordingly.
                            float comMult;
                            if (info.GetSubService() == ItemClass.SubService.CommercialHigh)
                            {
                                comMult = ACC_Options.Instance.HighCommercialGoodsMultiplier;
                                highComBuildingCount++;
                            }
                            else
                            { comMult = ACC_Options.Instance.CommercialGoodsMultiplier; }

                            if (comMult != 1.0f && amount < oldAmount)
                            {
                                int amountDelta = amount - oldAmount;
                                addAmount = System.Math.Abs(amountDelta) + (int)(amountDelta * comMult);

                                ResupplyBuilding(buildingId, ai, TransferManager.TransferReason.Goods, addAmount);

                                int curAmt = buildingManager.m_buildings.m_buffer[buildingId].m_customBuffer1;
                                comGoodsCount[buildingId] = curAmt;

                                if (ACC_Options.Instance.DebugConsolePrint)
                                {
                                    GoodsReplenishAmount += addAmount;
                                    GoodsOldTotalAmount += oldAmount;
                                    GoodsTotalAmount += curAmt;
                                    GoodsReadAmount++;
                                }
                            }
                            else
                            {
                                comGoodsCount[buildingId] = amount;
                                GoodsReadAmount++;
                            }
                        }
                        else if (ACC_Options.Instance.refillBuildingsEnable)
                        {
                            ResupplyBuilding(buildingId, ai, TransferManager.TransferReason.Goods, 10000);

                            if (ACC_Options.Instance.DebugConsolePrint)
                            {
                                int curAmt = buildingManager.m_buildings.m_buffer[buildingId].m_customBuffer1;

                                GoodsNewAmount++;
                                GoodsReplenishAmount += curAmt - amount;
                                GoodsOldTotalAmount += amount;
                                GoodsTotalAmount += curAmt;
                            }
                        }
                        else
                        {
                            comGoodsCount.Add(buildingId, amount);
                            GoodsNewAmount++;
                        }
                    }
                    else if (ai is IndustrialBuildingAI)
                    {
                        //Resupply building based on delta between current tick and last tick.
                        int addAmount = 0;
                        int amount = building.m_customBuffer1;

                        if (!ACC_Options.Instance.refillBuildingsEnable && indGoodsCount.TryGetValue(buildingId, out int oldAmount))
                        {
                            if (ACC_Options.Instance.IndustrialGoodsMultiplier != 1.0f && amount < oldAmount)
                            {
                                int amountDelta = amount - oldAmount;
                                addAmount = System.Math.Abs(amountDelta) + (int)(amountDelta * ACC_Options.Instance.IndustrialGoodsMultiplier);

                                ResupplyIndustrialBuilding(buildingId, ai, addAmount);

                                int curAmt = buildingManager.m_buildings.m_buffer[buildingId].m_customBuffer1;
                                indGoodsCount[buildingId] = curAmt;

                                if (ACC_Options.Instance.DebugConsolePrint)
                                {
                                    indGoodsReplenishAmount += addAmount;
                                    indGoodsOldTotalAmount += oldAmount;
                                    indGoodsTotalAmount += curAmt;
                                    indGoodsReadAmount++;
                                }
                            }
                            else
                            {
                                indGoodsCount[buildingId] = amount;
                                indGoodsReadAmount++;
                            }
                        }
                        else if (ACC_Options.Instance.refillBuildingsEnable)
                        {
                            ResupplyIndustrialBuilding(buildingId, ai, 10000);

                            if (ACC_Options.Instance.DebugConsolePrint)
                            {
                                int curAmt = buildingManager.m_buildings.m_buffer[buildingId].m_customBuffer1;

                                indGoodsNewAmount++;
                                indGoodsReplenishAmount += curAmt - amount;
                                indGoodsOldTotalAmount += amount;
                                indGoodsTotalAmount += curAmt;
                            }
                        }
                        else
                        {
                            indGoodsCount.Add(buildingId, amount);
                            indGoodsNewAmount++;
                        }
                    }
                }
            }
        }

        private void ResupplyBuilding(ushort buildingId, BuildingAI buildingAi, TransferManager.TransferReason goodsType, int transferAmount)
        {
            refTransferAmt = transferAmount;
            buildingAi.ModifyMaterialBuffer(buildingId, ref buildingManager.m_buildings.m_buffer[buildingId], goodsType, ref refTransferAmt);
        }

        private void ResupplyIndustrialBuilding(ushort buildingId, BuildingAI buildingAi, int transferAmount)
        {
            foreach (TransferManager.TransferReason type in industryGoods)
            { ResupplyBuilding(buildingId, buildingAi, type, transferAmount); }
        }
    }

    public class CoTimer : MonoBehaviour
    {
        private IEnumerator DebugTimer()
        {
            while (true)
            {
                yield return new WaitForSeconds(2.0f);

                if (ACC_Options.Instance.DebugConsolePrint)
                {
                    DebugOutputPanel.AddMessage(ColossalFramework.Plugins.PluginManager.MessageType.Message, "ACC - Com - Added/Old Total/Total/Reads/New: "
                        + string.Format("{0:n0}", GoodsMonitor.GoodsReplenishAmount) + "/"
                        + string.Format("{0:n0}", GoodsMonitor.GoodsOldTotalAmount) + "/"
                        + string.Format("{0:n0}", GoodsMonitor.GoodsTotalAmount) + "/"
                        + string.Format("{0:n0}", GoodsMonitor.GoodsReadAmount) + "/"
                        + string.Format("{0:n0}", GoodsMonitor.GoodsNewAmount) + "\n"

                        + "ACC - Ind - Added/Old Total/Total/Reads/New: "
                        + string.Format("{0:n0}", GoodsMonitor.indGoodsReplenishAmount) + "/"
                        + string.Format("{0:n0}", GoodsMonitor.indGoodsOldTotalAmount) + "/"
                        + string.Format("{0:n0}", GoodsMonitor.indGoodsTotalAmount) + "/"
                        + string.Format("{0:n0}", GoodsMonitor.indGoodsReadAmount) + "/"
                        + string.Format("{0:n0}", GoodsMonitor.indGoodsNewAmount) + "\n\n"
                        
                        + "ACC - Building Type Detection\nHi-D Com: "
                        + string.Format("{0:n0}", GoodsMonitor.highComBuildingCount));


                    GoodsMonitor.GoodsReplenishAmount = 0;
                    GoodsMonitor.GoodsOldTotalAmount = 0;
                    GoodsMonitor.GoodsTotalAmount = 0;
                    GoodsMonitor.GoodsReadAmount = 0;
                    GoodsMonitor.GoodsNewAmount = 0;
                    GoodsMonitor.indGoodsReplenishAmount = 0;
                    GoodsMonitor.indGoodsOldTotalAmount = 0;
                    GoodsMonitor.indGoodsTotalAmount = 0;
                    GoodsMonitor.indGoodsReadAmount = 0;
                    GoodsMonitor.indGoodsNewAmount = 0;
                    GoodsMonitor.highComBuildingCount = 0;
                }
            }
        }

        private IEnumerator DelayTimer()
        {
            while (true)
            {
                yield return new WaitForSeconds(ACC_Options.Instance.delayTimer);

                GoodsMonitor.startDelayed = true;
                DebugOutputPanel.AddMessage(ColossalFramework.Plugins.PluginManager.MessageType.Message, "ACC - Initialized.");

                StartCoroutine("DebugTimer");
                StopCoroutine("DelayTimer");
            }
        }
    }

    public class ACCLoader : LoadingExtensionBase
    {
        private CoTimer accTimerIns;

        public ACCLoader()
        { accTimerIns = Singleton<CoTimer>.instance; }

        public override void OnLevelUnloading()
        {
            base.OnLevelUnloading();

            GoodsMonitor.startDelayed = false;
            accTimerIns.StopCoroutine("DebugTimer");

            DebugOutputPanel.AddMessage(ColossalFramework.Plugins.PluginManager.MessageType.Message, "ACC - Deinitialized.");
        }

        public override void OnLevelLoaded(LoadMode mode)
        {
            base.OnLevelLoaded(mode);

            if (mode == LoadMode.LoadGame || mode == LoadMode.NewGame || mode == LoadMode.NewGameFromScenario)
            {
                accTimerIns.StartCoroutine("DelayTimer");
            }
        }
    }
}
