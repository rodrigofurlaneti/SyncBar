using SyncBar.Domain.Primitives;
namespace SyncBar.Domain.Entities
{
    public sealed class BranchPaymentMethodSetting : AggregateRoot
    {
        public long CompanyId { get; private set; }
        public long? BranchId { get; private set; }
        public bool EnablePix { get; private set; }
        public bool EnableBoleto { get; private set; }
        public bool EnableCreditCard { get; private set; }
        public bool EnableDebitCard { get; private set; }
        public bool EnableCashMachine { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? UpdatedAt { get; private set; }
        public bool IsActive { get; private set; }

        private BranchPaymentMethodSetting() : base(0) { }

        private BranchPaymentMethodSetting(
            long companyId,
            long? branchId,
            bool enablePix,
            bool enableBoleto,
            bool enableCreditCard,
            bool enableDebitCard,
            bool enableCashMachine,
            bool isActive) : base(0)
        {
            CompanyId = companyId;
            BranchId = branchId;
            EnablePix = enablePix;
            EnableBoleto = enableBoleto;
            EnableCreditCard = enableCreditCard;
            EnableDebitCard = enableDebitCard;
            EnableCashMachine = enableCashMachine;
            IsActive = isActive;
            CreatedAt = DateTime.UtcNow;
        }

        public static Result<BranchPaymentMethodSetting> Create(
            long companyId,
            long? branchId = null,
            bool enablePix = true,
            bool enableBoleto = true,
            bool enableCreditCard = true,
            bool enableDebitCard = true,
            bool enableCashMachine = true,
            bool isActive = true)
        {
            if (companyId <= 0)
                return Result.Failure<BranchPaymentMethodSetting>(
                    new Error("CompanyId.Invalid", "CompanyId inválido."));

            if (branchId.HasValue && branchId.Value <= 0)
                return Result.Failure<BranchPaymentMethodSetting>(
                    new Error("BranchId.Invalid", "BranchId inválido."));

            return Result.Success(new BranchPaymentMethodSetting(
                companyId,
                branchId,
                enablePix,
                enableBoleto,
                enableCreditCard,
                enableDebitCard,
                enableCashMachine,
                isActive));
        }

        public Result UpdateDetails(
            bool? enablePix = null,
            bool? enableBoleto = null,
            bool? enableCreditCard = null,
            bool? enableDebitCard = null,
            bool? enableCashMachine = null,
            bool? isActive = null)
        {
            if (enablePix.HasValue)
                EnablePix = enablePix.Value;

            if (enableBoleto.HasValue)
                EnableBoleto = enableBoleto.Value;

            if (enableCreditCard.HasValue)
                EnableCreditCard = enableCreditCard.Value;

            if (enableDebitCard.HasValue)
                EnableDebitCard = enableDebitCard.Value;

            if (enableCashMachine.HasValue)
                EnableCashMachine = enableCashMachine.Value;

            if (isActive.HasValue)
                IsActive = isActive.Value;

            UpdatedAt = DateTime.UtcNow;

            return Result.Success();
        }

        public void Deactivate()
        {
            IsActive = false;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Activate()
        {
            IsActive = true;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
