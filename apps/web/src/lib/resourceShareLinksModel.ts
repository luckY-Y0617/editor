import type { CreateShareLinkRequest, ShareLinkContentProtectionDto } from "./appApi";
import {
  createShareLinkRequest,
  defaultPublicSharePolicy,
  getPublicSharePolicyViolation,
  type PublicSharePolicy,
} from "./documentShareLinksModel";

export type ResourceShareType = "collection" | "library";

export type PublicResourceShareDraft = {
  contentProtection: ShareLinkContentProtectionDto;
  expiresAt: string | null;
  password: string | null;
  passwordEnabled: boolean;
  resourceId: string;
  resourceType: ResourceShareType;
};

export function getContainerPublicSharePolicy(resourceType: ResourceShareType): PublicSharePolicy {
  return resourceType === "library"
    ? { ...defaultPublicSharePolicy, allowLibraryScope: true }
    : defaultPublicSharePolicy;
}

export function getResourceShareTitle(resourceType: ResourceShareType, title: string) {
  const safeTitle = title.trim() || (resourceType === "library" ? "Untitled Library" : "Untitled Folder");
  return resourceType === "library" ? `Publish Library "${safeTitle}"` : `Share Folder "${safeTitle}"`;
}

export function getPublicResourceShareDisabledReason(
  draft: PublicResourceShareDraft,
  policy: PublicSharePolicy = getContainerPublicSharePolicy(draft.resourceType),
) {
  if (!draft.resourceId.trim()) {
    return draft.resourceType === "library"
      ? "Library context is not available."
      : "Folder context is not available.";
  }

  return getPublicSharePolicyViolation({
    collectionId: draft.resourceType === "collection" ? draft.resourceId : null,
    contentProtection: draft.contentProtection,
    expiresAt: draft.expiresAt,
    libraryId: draft.resourceType === "library" ? draft.resourceId : null,
    password: draft.password,
    passwordEnabled: draft.passwordEnabled,
    policy,
    scope: draft.resourceType,
  });
}

export function createPublicResourceShareLinkRequest(draft: PublicResourceShareDraft): {
  request: CreateShareLinkRequest;
  resourceId: string;
  resourceType: ResourceShareType;
} {
  return {
    request: createShareLinkRequest({
      audience: "public",
      contentProtection: draft.contentProtection,
      expiresAt: draft.expiresAt,
      password: draft.passwordEnabled ? draft.password : null,
      roleKey: "viewer",
      subjectEmail: null,
    }),
    resourceId: draft.resourceId,
    resourceType: draft.resourceType,
  };
}
