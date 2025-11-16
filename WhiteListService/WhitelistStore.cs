using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace WhiteListService
{
    [DataContract]
    public class WhitelistStore
    {
        [DataMember(Name = "domains")]
        public string[] Domains { get; set; } = Array.Empty<string>();

        public WhitelistStore()
        {
        }

        public WhitelistStore(string[] domains)
        {
            if (domains == null)
            {
                Domains = Array.Empty<string>();
            }
            else
            {
                Domains = NormalizeDomains(domains);
            }
        }

        public void AddDomain(string domain)
        {
            if (string.IsNullOrWhiteSpace(domain))
            {
                throw new ArgumentNullException(nameof(domain));
            }

            var normalizedDomain = NormalizeDomain(domain);
            if (!Domains.Contains(normalizedDomain, StringComparer.OrdinalIgnoreCase))
            {
                var list = new List<string>(Domains) { normalizedDomain };
                Domains = list.OrderBy(d => d, StringComparer.OrdinalIgnoreCase).ToArray();
            }
        }

        public void RemoveDomain(string domain)
        {
            if (string.IsNullOrWhiteSpace(domain))
            {
                return;
            }

            var normalizedDomain = NormalizeDomain(domain);
            Domains = Domains
                .Where(d => !string.Equals(d, normalizedDomain, StringComparison.OrdinalIgnoreCase))
                .ToArray();
        }

        public bool ContainsDomain(string domain)
        {
            if (string.IsNullOrWhiteSpace(domain))
            {
                return false;
            }

            var normalizedDomain = NormalizeDomain(domain);
            return Domains.Any(d => string.Equals(d, normalizedDomain, StringComparison.OrdinalIgnoreCase));
        }

        public void Clear()
        {
            Domains = Array.Empty<string>();
        }

        public int Count => Domains?.Length ?? 0;

        public bool IsEmpty => Count == 0;

        public IReadOnlyList<string> GetDomains()
        {
            return Domains?.ToList().AsReadOnly() ?? new List<string>().AsReadOnly();
        }

        private static string[] NormalizeDomains(string[] domains)
        {
            if (domains == null)
            {
                return Array.Empty<string>();
            }

            return domains
                .Where(d => !string.IsNullOrWhiteSpace(d))
                .Select(NormalizeDomain)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(d => d, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        private static string NormalizeDomain(string domain)
        {
            if (string.IsNullOrWhiteSpace(domain))
            {
                return string.Empty;
            }

            return domain.Trim().ToLowerInvariant();
        }
    }
}
