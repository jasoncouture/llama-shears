# docker buildx bake auto-loads this file, plus an optional sibling
# `docker-bake.override.hcl` (gitignored). Use the override file for
# per-host registry tags so your push target stays out of the repo.

variable "IMAGE_TAG" {
  default = "llama-shears:dev"
}

target "llamashears" {
  context    = "."
  dockerfile = "Dockerfile"
  tags       = [IMAGE_TAG]
}

group "default" {
  targets = ["llamashears"]
}
