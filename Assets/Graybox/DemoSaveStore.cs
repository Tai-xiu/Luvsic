using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace Luvsic.Graybox
{
    [Serializable]
    internal sealed class DemoInventory
    {
        public int[] counts = new int[DemoItemCatalog.Items.Length];

        public int Count(DemoItem item) => counts[(int)item];

        public void Add(DemoItem item, int amount)
        {
            counts[(int)item] += amount;
        }

        public int Slots(DemoItemCatalog catalog)
        {
            int slots = 0;
            foreach (DemoItem item in DemoItemCatalog.Items)
            {
                int stackSize = catalog[item].stackSize;
                slots += (Count(item) + stackSize - 1) / stackSize;
            }
            return slots;
        }

        public bool CanAdd(DemoItem item, DemoItemCatalog catalog)
        {
            return Count(item) % catalog[item].stackSize != 0 || Slots(catalog) < GrayboxFlow.PackCapacity;
        }

        public DemoInventory Copy()
        {
            return new DemoInventory { counts = (int[])counts.Clone() };
        }

        public bool IsValid()
        {
            if (counts == null || counts.Length != DemoItemCatalog.Items.Length)
                return false;
            foreach (int count in counts)
                if (count < 0 || count > 1000000)
                    return false;
            return true;
        }
    }

    [Serializable]
    internal sealed class DemoProfile
    {
        public int version = 1;
        public int coins = 45;
        public DemoInventory warehouse = new DemoInventory();
        public DemoInventory prepared = new DemoInventory();
        public bool sword;
        public bool armor;
        public string pendingRun = "";

        public DemoProfile Copy()
        {
            return new DemoProfile
            {
                coins = coins,
                warehouse = warehouse.Copy(),
                prepared = prepared.Copy(),
                sword = sword,
                armor = armor,
                pendingRun = pendingRun
            };
        }

        public bool IsValid()
        {
            return version == 1 && coins >= 0 && warehouse != null && warehouse.IsValid()
                && prepared != null && prepared.IsValid() && pendingRun != null;
        }
    }

    internal sealed class DemoSaveStore
    {
        private readonly string path;
        private string BackupPath => path + ".bak";
        private bool primaryValid;

        public DemoSaveStore(string directory)
        {
            path = Path.Combine(directory, "progress.json");
        }

        public DemoProfile Load(out string notice)
        {
            notice = null;
            primaryValid = TryRead(path, out DemoProfile profile);
            if (primaryValid)
                return profile;

            if (TryRead(BackupPath, out profile))
            {
                notice = "已从备份恢复存档。";
                return profile;
            }

            if (!File.Exists(path) && !File.Exists(BackupPath))
                return new DemoProfile();

            notice = "存档无法读取。可保留原文件并创建新档。";
            return null;
        }

        public bool Save(DemoProfile profile, out string error)
        {
            string temporary = path + ".tmp";
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(temporary, JsonUtility.ToJson(profile, true), new UTF8Encoding(false));
                if (File.Exists(path))
                    File.Replace(temporary, path, primaryValid ? BackupPath : null);
                else
                    File.Move(temporary, path);

                primaryValid = true;
                error = null;
                return true;
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                Debug.LogWarning("Luvsic save: " + exception.Message);
                error = "保存失败，请检查磁盘或文件权限后重试。";
                return false;
            }
        }

        public bool CreateFresh(DemoProfile profile, out string error)
        {
            string suffix = ".invalid-" + Guid.NewGuid().ToString("N");
            try
            {
                if (File.Exists(path))
                    File.Copy(path, path + suffix);
                if (File.Exists(BackupPath))
                    File.Copy(BackupPath, BackupPath + suffix);
                return Save(profile, out error);
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                Debug.LogWarning("Luvsic save archive: " + exception.Message);
                error = "无法保留原存档。请检查磁盘或文件权限后重试。";
                return false;
            }
        }

        private static bool TryRead(string file, out DemoProfile profile)
        {
            try
            {
                profile = File.Exists(file) ? JsonUtility.FromJson<DemoProfile>(File.ReadAllText(file)) : null;
                return profile != null && profile.IsValid();
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is ArgumentException)
            {
                profile = null;
                return false;
            }
        }
    }
}
