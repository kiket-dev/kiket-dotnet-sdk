using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Kiket.SDK
{
    /// <summary>
    /// Client for blockchain audit verification operations.
    /// </summary>
    public class AuditClient
    {
        private readonly KiketClient _client;

        public AuditClient(KiketClient client)
        {
            _client = client;
        }

        /// <summary>
        /// List blockchain anchors for the organization.
        /// </summary>
        public async Task<ListAnchorsResult> ListAnchorsAsync(ListAnchorsOptions? options = null)
        {
            options ??= new ListAnchorsOptions();
            var queryParams = new List<string>
            {
                $"page={options.Page}",
                $"per_page={options.PerPage}"
            };

            if (!string.IsNullOrEmpty(options.Status))
                queryParams.Add($"status={options.Status}");
            if (!string.IsNullOrEmpty(options.Network))
                queryParams.Add($"network={options.Network}");
            if (options.From.HasValue)
                queryParams.Add($"from={options.From.Value:O}");
            if (options.To.HasValue)
                queryParams.Add($"to={options.To.Value:O}");

            var query = string.Join("&", queryParams);
            var response = await _client.GetAsync($"/api/v1/audit/anchors?{query}");
            return JsonSerializer.Deserialize<ListAnchorsResult>(response)!;
        }

        /// <summary>
        /// Get details of a specific anchor by merkle root.
        /// </summary>
        public async Task<BlockchainAnchor> GetAnchorAsync(string merkleRoot, bool includeRecords = false)
        {
            var query = includeRecords ? "?include_records=true" : "";
            var response = await _client.GetAsync($"/api/v1/audit/anchors/{merkleRoot}{query}");
            return JsonSerializer.Deserialize<BlockchainAnchor>(response)!;
        }

        /// <summary>
        /// Get the blockchain proof for a specific audit record.
        /// </summary>
        public async Task<BlockchainProof> GetProofAsync(long recordId)
        {
            var response = await _client.GetAsync($"/api/v1/audit/records/{recordId}/proof");
            return JsonSerializer.Deserialize<BlockchainProof>(response)!;
        }

        /// <summary>
        /// Verify a blockchain proof via the API.
        /// </summary>
        public async Task<VerificationResult> VerifyAsync(BlockchainProof proof)
        {
            var payload = new
            {
                content_hash = proof.ContentHash,
                merkle_root = proof.MerkleRoot,
                proof = proof.Proof,
                leaf_index = proof.LeafIndex,
                tx_hash = proof.TxHash
            };

            var response = await _client.PostAsync("/api/v1/audit/verify", JsonSerializer.Serialize(payload));
            return JsonSerializer.Deserialize<VerificationResult>(response)!;
        }

        /// <summary>
        /// Compute the content hash for a record (for local verification).
        /// </summary>
        public static string ComputeContentHash(Dictionary<string, object> data)
        {
            var sorted = data.OrderBy(kv => kv.Key).ToDictionary(kv => kv.Key, kv => kv.Value);
            var canonical = JsonSerializer.Serialize(sorted);
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(canonical));
            return "0x" + BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }

        /// <summary>
        /// Verify a Merkle proof locally without making an API call.
        /// </summary>
        public static bool VerifyProofLocally(
            string contentHash,
            List<string> proofPath,
            int leafIndex,
            string merkleRoot)
        {
            var current = NormalizeHash(contentHash);
            var idx = leafIndex;

            foreach (var siblingHex in proofPath)
            {
                var sibling = NormalizeHash(siblingHex);
                current = idx % 2 == 0
                    ? HashPair(current, sibling)
                    : HashPair(sibling, current);
                idx /= 2;
            }

            var expected = NormalizeHash(merkleRoot);
            return current.SequenceEqual(expected);
        }

        private static byte[] NormalizeHash(string h)
        {
            var hex = h.StartsWith("0x") ? h.Substring(2) : h;
            var bytes = new byte[hex.Length / 2];
            for (var i = 0; i < bytes.Length; i++)
            {
                bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            }
            return bytes;
        }

        private static byte[] HashPair(byte[] left, byte[] right)
        {
            // Sort for consistent ordering
            if (CompareBytes(left, right) > 0)
            {
                (left, right) = (right, left);
            }

            using var sha256 = SHA256.Create();
            var combined = new byte[left.Length + right.Length];
            left.CopyTo(combined, 0);
            right.CopyTo(combined, left.Length);
            return sha256.ComputeHash(combined);
        }

        private static int CompareBytes(byte[] a, byte[] b)
        {
            for (var i = 0; i < Math.Min(a.Length, b.Length); i++)
            {
                var cmp = a[i].CompareTo(b[i]);
                if (cmp != 0) return cmp;
            }
            return a.Length.CompareTo(b.Length);
        }
    }

    public class ListAnchorsOptions
    {
        public string? Status { get; set; }
        public string? Network { get; set; }
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public int Page { get; set; } = 1;
        public int PerPage { get; set; } = 25;
    }

    public class ListAnchorsResult
    {
        [JsonPropertyName("anchors")]
        public List<BlockchainAnchor> Anchors { get; set; } = new();

        [JsonPropertyName("pagination")]
        public PaginationInfo Pagination { get; set; } = new();
    }

    public class PaginationInfo
    {
        [JsonPropertyName("page")]
        public int Page { get; set; }

        [JsonPropertyName("per_page")]
        public int PerPage { get; set; }

        [JsonPropertyName("total")]
        public int Total { get; set; }

        [JsonPropertyName("total_pages")]
        public int TotalPages { get; set; }
    }

    public class BlockchainAnchor
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("merkle_root")]
        public string MerkleRoot { get; set; } = "";

        [JsonPropertyName("leaf_count")]
        public int LeafCount { get; set; }

        [JsonPropertyName("first_record_at")]
        public string? FirstRecordAt { get; set; }

        [JsonPropertyName("last_record_at")]
        public string? LastRecordAt { get; set; }

        [JsonPropertyName("network")]
        public string Network { get; set; } = "";

        [JsonPropertyName("status")]
        public string Status { get; set; } = "";

        [JsonPropertyName("tx_hash")]
        public string? TxHash { get; set; }

        [JsonPropertyName("block_number")]
        public long? BlockNumber { get; set; }

        [JsonPropertyName("block_timestamp")]
        public string? BlockTimestamp { get; set; }

        [JsonPropertyName("confirmed_at")]
        public string? ConfirmedAt { get; set; }

        [JsonPropertyName("explorer_url")]
        public string? ExplorerUrl { get; set; }

        [JsonPropertyName("created_at")]
        public string? CreatedAt { get; set; }

        [JsonPropertyName("records")]
        public List<AnchorRecord>? Records { get; set; }
    }

    public class AnchorRecord
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; } = "";

        [JsonPropertyName("leaf_index")]
        public int LeafIndex { get; set; }

        [JsonPropertyName("content_hash")]
        public string ContentHash { get; set; } = "";
    }

    public class BlockchainProof
    {
        [JsonPropertyName("record_id")]
        public long RecordId { get; set; }

        [JsonPropertyName("record_type")]
        public string RecordType { get; set; } = "";

        [JsonPropertyName("content_hash")]
        public string ContentHash { get; set; } = "";

        [JsonPropertyName("anchor_id")]
        public long AnchorId { get; set; }

        [JsonPropertyName("merkle_root")]
        public string MerkleRoot { get; set; } = "";

        [JsonPropertyName("leaf_index")]
        public int LeafIndex { get; set; }

        [JsonPropertyName("leaf_count")]
        public int LeafCount { get; set; }

        [JsonPropertyName("proof")]
        public List<string> Proof { get; set; } = new();

        [JsonPropertyName("network")]
        public string Network { get; set; } = "";

        [JsonPropertyName("tx_hash")]
        public string? TxHash { get; set; }

        [JsonPropertyName("block_number")]
        public long? BlockNumber { get; set; }

        [JsonPropertyName("block_timestamp")]
        public string? BlockTimestamp { get; set; }

        [JsonPropertyName("verified")]
        public bool Verified { get; set; }

        [JsonPropertyName("verification_url")]
        public string? VerificationUrl { get; set; }
    }

    public class VerificationResult
    {
        [JsonPropertyName("verified")]
        public bool Verified { get; set; }

        [JsonPropertyName("proof_valid")]
        public bool ProofValid { get; set; }

        [JsonPropertyName("blockchain_verified")]
        public bool BlockchainVerified { get; set; }

        [JsonPropertyName("content_hash")]
        public string ContentHash { get; set; } = "";

        [JsonPropertyName("merkle_root")]
        public string MerkleRoot { get; set; } = "";

        [JsonPropertyName("leaf_index")]
        public int LeafIndex { get; set; }

        [JsonPropertyName("block_number")]
        public long? BlockNumber { get; set; }

        [JsonPropertyName("block_timestamp")]
        public string? BlockTimestamp { get; set; }

        [JsonPropertyName("network")]
        public string? Network { get; set; }

        [JsonPropertyName("explorer_url")]
        public string? ExplorerUrl { get; set; }

        [JsonPropertyName("error")]
        public string? Error { get; set; }
    }
}
