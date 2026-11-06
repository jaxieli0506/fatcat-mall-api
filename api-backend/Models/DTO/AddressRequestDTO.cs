using System.ComponentModel.DataAnnotations;

namespace api_backend.Models.DTO
{
    public class AddressRequestDTO : IValidatableObject
    {
        [Required(ErrorMessage = "收件人姓名為必填欄位")]
        [StringLength(50, ErrorMessage = "收件人姓名不能超過50個字元")]
        public string RecipientName { get; set; }

        [Required(ErrorMessage = "電話號碼為必填欄位")]
        [StringLength(20, ErrorMessage = "電話號碼不能超過20個字元")]
        public string PhoneNumber { get; set; }

        [Required(ErrorMessage = "地址類型為必填欄位")]
        [Range(0, 1, ErrorMessage = "地址類型必須為0(一般宅配地址)或1(超商取貨地址)")]
        public int AddressType { get; set; }

        // 超商相關欄位（當 AddressType = 1 時必填）
        [Range(0, 3, ErrorMessage = "超商類型必須為0(7-Eleven)、1(FamilyMart)、2(OK)、3(Hi-Life)")]
        public int? StoreType { get; set; }
        public string? StoreName { get; set; }
        public string? StoreBranch { get; set; }

        [Required(ErrorMessage = "城市為必填欄位")]
        [StringLength(10, ErrorMessage = "城市名稱不能超過10個字元")]
        public string City { get; set; }

        [Required(ErrorMessage = "區域為必填欄位")]
        [StringLength(20, ErrorMessage = "區域名稱不能超過20個字元")]
        public string District { get; set; }

        [Required(ErrorMessage = "詳細地址為必填欄位")]
        [StringLength(100, ErrorMessage = "詳細地址不能超過100個字元")]
        public string AddressDetail { get; set; }

        public bool IsDefault { get; set; } = false;

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();

            // 當地址類型為超商時，驗證超商相關欄位
            if (AddressType == 1)
            {
                if (!StoreType.HasValue)
                {
                    results.Add(new ValidationResult("超商類型為必填欄位", new[] { nameof(StoreType) }));
                }
                else if (StoreType.Value < 0 || StoreType.Value > 3)
                {
                    results.Add(new ValidationResult("超商類型必須為0(7-Eleven)、1(FamilyMart)、2(OK)、3(Hi-Life)", new[] { nameof(StoreType) }));
                }

                if (string.IsNullOrEmpty(StoreName))
                {
                    results.Add(new ValidationResult("超商名稱為必填欄位", new[] { nameof(StoreName) }));
                }

                // 把超商分店為必填欄位註解掉, 因為我不想讓它為必填
                //if (string.IsNullOrEmpty(StoreBranch))
                //{
                //    results.Add(new ValidationResult("超商分店為必填欄位", new[] { nameof(StoreBranch) }));
                //}
            }

            return results;
        }
    }
} 