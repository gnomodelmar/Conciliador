using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ConciliadorBancario.Data;
using ConciliadorBancario.Models;

namespace ConciliadorBancario.Services
{
    public class ConciliacionEngine
    {
        private readonly MovimientoRepository _movRepo;

        public ConciliacionEngine()
        {
            _movRepo = new MovimientoRepository();
        }

        public void AutoConciliar()
        {
            var bancos = _movRepo.GetPendientes("Banco");
            var sistemas = _movRepo.GetPendientes("Sistema");

            foreach (var banco in bancos)
            {
                if (banco.Estado == EstadosMovimiento.PendienteAsientoMasivo) continue;

                Movimiento match = null;

                // 1. Auto-Match by Operation Code & Amount
                // Enforces that Bank tags match.
                if (!string.IsNullOrWhiteSpace(banco.Referencia_CodOperacion))
                {
                    string cleanBancoCod = banco.Referencia_CodOperacion.TrimStart('0');
                    if (string.IsNullOrEmpty(cleanBancoCod)) cleanBancoCod = "0";

                    match = sistemas.FirstOrDefault(s =>
                        s.Banco == banco.Banco &&
                        !string.IsNullOrWhiteSpace(s.CodOperacionSistema) &&
                        s.CodOperacionSistema.TrimStart('0').Equals(cleanBancoCod, StringComparison.OrdinalIgnoreCase) &&
                        Math.Abs(s.Monto - banco.Monto) < 0.01 &&
                        s.Estado == EstadosMovimiento.NoEncontrado);

                    if (match != null)
                    {
                        MarcarConciliado(banco, match, $"Auto Match por Cód. Operación Sist:[{match.CodOperacionSistema}] Banco:[{banco.Referencia_CodOperacion}]");
                        sistemas.Remove(match);
                        continue;
                    }
                }

                // 2. Specific Rule: Transferencia Interna (RI ARMOBAR)
                if (banco.Concepto != null && banco.Concepto.IndexOf("RI ARMOBAR", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    match = sistemas.FirstOrDefault(s =>
                        s.Banco == banco.Banco &&
                        Math.Abs(s.Monto - banco.Monto) < 0.01 &&
                        s.Fecha.Date == banco.Fecha.Date &&
                        s.Concepto != null && s.Concepto.IndexOf("Transferencia interna", StringComparison.OrdinalIgnoreCase) >= 0 &&
                        s.Estado == EstadosMovimiento.NoEncontrado);

                    if (match != null)
                    {
                        MarcarConciliado(banco, match, $"Auto Match Transferencia Interna");
                        sistemas.Remove(match);
                        continue;
                    }
                }

                // 3. Regular auto-match (misma fecha, mismo monto, MISMO BANCO, AND AT LEAST 1 WORD MATCH)
                var origenWords = GetSignificantWords(banco.Concepto);
                match = sistemas.FirstOrDefault(s =>
                    s.Banco == banco.Banco &&
                    Math.Abs(s.Monto - banco.Monto) < 0.01 &&
                    s.Fecha.Date == banco.Fecha.Date &&
                    s.Estado == EstadosMovimiento.NoEncontrado &&
                    CountWordOverlap(origenWords, s.Concepto) > 0);

                if (match != null)
                {
                    MarcarConciliado(banco, match, $"Auto Match Exacto (Monto, Fecha, Palabra Clave)");
                    sistemas.Remove(match);
                }
            }

            // Asientos Masivos Match
            bancos = _movRepo.GetPendientes("Banco");
            var masivosSistema = sistemas.Where(s => s.Concepto != null && s.Concepto.Contains("[AM-")).ToList();

            foreach(var sys in masivosSistema)
            {
                var bancoMatch = bancos.FirstOrDefault(b =>
                    b.Banco == sys.Banco &&
                    b.Estado == EstadosMovimiento.PendienteAsientoMasivo &&
                    Math.Abs(sys.Monto - b.Monto) < 0.01 &&
                    b.Fecha.Date == sys.Fecha.Date);

                if(bancoMatch != null)
                {
                    MarcarConciliado(bancoMatch, sys, $"Auto Match Asiento Masivo Sist:[{sys.CodOperacionSistema}]");
                    bancos.Remove(bancoMatch);
                }
            }

            bancos = _movRepo.GetPendientes("Banco");
            foreach (var banco in bancos)
            {
                if (banco.Estado == EstadosMovimiento.NoEncontrado)
                {
                    var posibles = ObtenerCandidatosFuzzy(banco, sistemas);
                    if (posibles.Count > 0)
                    {
                        _movRepo.UpdateEstado(banco.Id, EstadosMovimiento.PosibleMatch, "Se detectaron posibles matches manuales", null);
                    }
                }
            }
        }

        public List<Movimiento> ObtenerCandidatosFuzzy(Movimiento origen, List<Movimiento> posibles)
        {
            var origenWords = GetSignificantWords(origen.Concepto);

            return posibles.Where(p =>
                (p.Estado == EstadosMovimiento.NoEncontrado || p.Estado == EstadosMovimiento.PosibleMatch) &&
                ((Math.Abs(p.Monto - origen.Monto) < 0.01 && Math.Abs((p.Fecha - origen.Fecha).TotalDays) <= 7) ||
                 (Math.Abs(p.Monto - origen.Monto) <= 1.00 && p.Fecha.Date == origen.Fecha.Date))
                )
                .OrderByDescending(p => CountWordOverlap(origenWords, p.Concepto))
                .ThenByDescending(p => p.Banco == origen.Banco)
                .ThenBy(p => Math.Abs(p.Monto - origen.Monto))
                .ThenBy(p => Math.Abs((p.Fecha - origen.Fecha).TotalDays))
                .ToList();
        }

        private HashSet<string> GetSignificantWords(string text)
        {
            var words = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(text)) return words;

            var matches = Regex.Matches(text, @"\b\w{3,}\b");
            foreach (Match match in matches)
            {
                words.Add(match.Value);
            }
            return words;
        }

        private int CountWordOverlap(HashSet<string> targetWords, string textToTest)
        {
            if (string.IsNullOrWhiteSpace(textToTest) || targetWords.Count == 0) return 0;

            var testWords = GetSignificantWords(textToTest);
            int overlap = 0;
            foreach(var w in testWords)
            {
                if (targetWords.Contains(w)) overlap++;
            }
            return overlap;
        }

        public void MarcarConciliado(Movimiento banco, Movimiento sistema, string obs)
        {
            _movRepo.UpdateEstado(banco.Id, EstadosMovimiento.Conciliado, obs, sistema.Id);
            _movRepo.UpdateEstado(sistema.Id, EstadosMovimiento.Conciliado, obs, banco.Id);
        }

        public void MarcarConciliadoMulti(List<Movimiento> bancos, List<Movimiento> sistemas, string obs)
        {
            string grupoId = Guid.NewGuid().ToString();
            _movRepo.UpdateEstadoMulti(bancos.Select(b => b.Id).ToList(), EstadosMovimiento.Conciliado, obs, grupoId);
            _movRepo.UpdateEstadoMulti(sistemas.Select(s => s.Id).ToList(), EstadosMovimiento.Conciliado, obs, grupoId);
        }
    }
}
