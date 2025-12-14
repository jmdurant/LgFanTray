using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace LgFanTray
{
    public class EcProbeClient
    {
        private static readonly Regex HexRegex = new Regex(@"0x(?<hex>[0-9A-Fa-f]{2})", RegexOptions.Compiled);

        private readonly string ecProbePath;

        public EcProbeClient(string ecProbePath)
        {
            this.ecProbePath = ecProbePath;
        }

        private string ResolveEcProbePath(out string triedPaths)
        {
            triedPaths = null;

            if (string.IsNullOrWhiteSpace(ecProbePath))
            {
                return null;
            }

            string baseDir = AppContext.BaseDirectory;

            string[] candidates;
            if (Path.IsPathRooted(ecProbePath))
            {
                candidates = new[]
                {
                    ecProbePath,
                    Path.Combine(baseDir, Path.GetFileName(ecProbePath)),
                    Path.Combine(baseDir, "ec-probe.exe")
                };
            }
            else
            {
                candidates = new[]
                {
                    Path.Combine(baseDir, ecProbePath),
                    Path.Combine(baseDir, "ec-probe.exe")
                };
            }

            triedPaths = string.Join("; ", candidates);

            foreach (var c in candidates)
            {
                if (!string.IsNullOrWhiteSpace(c) && File.Exists(c))
                {
                    return c;
                }
            }

            return candidates.Length > 0 ? candidates[0] : null;
        }

        public bool TryReadRegister(byte register, out byte value, out string error)
        {
            value = 0;
            error = null;

            string triedPaths;
            string exe = ResolveEcProbePath(out triedPaths);

            if (string.IsNullOrWhiteSpace(exe) || !File.Exists(exe))
            {
                error = "ec-probe.exe not found. Tried: " + triedPaths;
                return false;
            }

            string output;
            if (!TryRun(exe, string.Format(CultureInfo.InvariantCulture, "read 0x{0:X2}", register), out output, out error))
            {
                return false;
            }

            var m = HexRegex.Match(output ?? string.Empty);
            if (!m.Success)
            {
                error = "Could not parse ec-probe output: " + (output ?? string.Empty).Trim();
                return false;
            }

            value = byte.Parse(m.Groups["hex"].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            return true;
        }

        public bool TryWriteRegister(byte register, byte newValue, out string output, out string error)
        {
            output = null;
            error = null;

            string triedPaths;
            string exe = ResolveEcProbePath(out triedPaths);

            if (string.IsNullOrWhiteSpace(exe) || !File.Exists(exe))
            {
                error = "ec-probe.exe not found. Tried: " + triedPaths;
                return false;
            }

            return TryRun(
                exe,
                string.Format(CultureInfo.InvariantCulture, "write 0x{0:X2} 0x{1:X2} -v", register, newValue),
                out output,
                out error);
        }

        private static bool TryRun(string exePath, string arguments, out string output, out string error)
        {
            output = null;
            error = null;

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = exePath,
                    Arguments = arguments,
                    WorkingDirectory = Path.GetDirectoryName(exePath),
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (var proc = Process.Start(psi))
                {
                    bool exited = proc.WaitForExit(5000);
                    output = proc.StandardOutput.ReadToEnd();
                    string err = proc.StandardError.ReadToEnd();

                    if (!exited)
                    {
                        try { proc.Kill(); } catch { }
                        error = "ec-probe timed out";
                        return false;
                    }

                    if (!string.IsNullOrWhiteSpace(err))
                    {
                        error = err.Trim();
                        return false;
                    }

                    if (proc.ExitCode != 0)
                    {
                        if (string.IsNullOrWhiteSpace(error))
                        {
                            error = "ec-probe exited with code " + proc.ExitCode;
                        }

                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }
    }
}
