# Version 3 Scope and Workflow

## Applications

1. Application Portal: displays only applications assigned to the signed-in user.
2. BC Administration: Application Catalog, Users & Roles and Master Data.
3. BC Fixed Asset: Asset Survey, Asset Manager Review, Finance Review, Asset Register, Numbering and Reports.

## Asset Survey workflow

`Draft/Returned -> ManagerReview -> FinanceReview -> Registered`

- Asset Manager and Finance can Return a record with a mandatory reason.
- Finance confirms classification and generates the permanent Fixed Asset number from the active numbering scheme.
- Returned records are editable by their creator (or an administrator) and retain approval comments and audit evidence.
- Each survey accepts Actual, Serial and Other images; phone browsers may open the camera from the file selector.
- Lists use location names, thumbnails and creator names; selecting the survey number opens its detail popup.
- Database rowversion fields support optimistic concurrency for future edit-conflict handling.

## Excel source mapping

The `NEXT Asset Survey` source sheet supplied the Phase 1 field model. The implementation retains the core fields: survey date, surveyor, department, custodian, location, asset name, category, brand, model, serial number, ownership, condition, dimensions, weight, photographs, missing-data reason, purchase evidence, purchase date/value, reviewer result, finance category, duplicate check and permanent asset ID.

The approved UI additions are also included: quantity, UOM, received date, PO number, Export and QR tag printing. Move Plan and all relocation/lifecycle sheets are excluded from this version.

## Deferred modules

- Transfer & Movement
- Maintenance
- Disposal / Write-off
- Physical Verification
- Packing, Manifest, Receiving and relocation close-out
