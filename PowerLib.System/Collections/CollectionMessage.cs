namespace PowerLib.System.Collections;

internal enum CollectionMessage
{
  CollectionElementError,
  EnumeratorPositionAfterLast,
  EnumeratorPositionBeforeFirst,
  InternalCollectionDoesNotSupportCapacity,
  InternalCollectionDoesNotSupportStamp,
  InternalCollectionIsRestricted,

  //InternalCollectionHasFixedLayout,
  //InternalCollectionHasFixedSize,
  //InternalCollectionHasReadOnlyValue,

  InternalCollectionIsEmpty,
  InternalCollectionIsFixed,
  InternalCollectionIsReadOnly,
  InternalCollectionNodeIsLeaf,
  InternalCollectionSlotIsNotEmpty,
  InternalCollectionWasModified,
  NoElementMatchedInPredicate,
  OneMoreElementMatchedInPredicate,
  SourceCollectionHasOneMoreElements,
  SourceCollectionIsEmpty,
  DuplicateCollectionElement,
  KeyAlreadyExists,
}
