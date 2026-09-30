namespace Diagnostic.Api.Common;

// Enums are stored as strings in the database so the tables stay readable.

public enum Gender { Male, Female, Other }

public enum InvestigationStatus { Active, PartiallyCancelled, Cancelled }

/// <summary>
/// Ordered → SampleCollected → SampleReceived → ResultEntered → Verified (lab) → Finalized (pathologist) → Delivered.
/// SampleRejected = the lab rejected the tube; the test waits in "Repeat Sample" for a new collection.
/// </summary>
public enum TestStatus { Ordered, SampleCollected, SampleReceived, ResultEntered, Verified, Delivered, Cancelled, SampleRejected, Finalized }

public enum TestResultType { Parameter, Text, Radiology }

public enum ParameterInputType { Numeric, Text, Option }

public enum PaymentType { Payment, DueCollection, Refund }

public enum DiscountType { Percent, Flat }

public enum CancelStatus { Pending, Approved, Rejected }

/// <summary>Collected (collection room) → Dispatched (sent to lab) → Received (acknowledged by lab) or Rejected.</summary>
public enum SampleStatus { Collected, Dispatched, Received, Rejected }

public enum PrinterType { A4, Sticker, Pos }

public enum ResultFlag { Normal, Low, High }

public enum PermissionAction { View, Add, Edit, Delete, Print }
