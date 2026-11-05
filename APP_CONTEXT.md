# Custom Soundboard: App Context

## Purpose of this document

This document provides shared product context for Claude, Codex, and GitHub Copilot. Read it before suggesting or implementing changes to the app.

It describes the motivation, intended users, and planned editions of the product. It deliberately does not define architecture, frameworks, APIs, storage, pricing infrastructure, or other implementation details. Those decisions will be covered in a separate implementation prompt.

## Why this app exists

Custom Soundboard is inspired by the creator's younger brother, who has non-verbal autism and Down syndrome.

The aim is to create a free, customisable soundboard that can help people communicate by tapping clear, familiar tiles that play words or phrases. The app is intended for publication on the Google Play Store so that it can be useful to other individuals, families, carers, and support networks without a purchase barrier.

This is an accessibility-focused communication tool. Changes should support independence, dignity, clarity, and ease of use. The people using the soundboard may have a wide range of needs, reading abilities, motor skills, and experience with technology.

## Product vision

The app should allow a parent, carer, family member, or other supporting user to customise soundboard tiles in an edit mode. They should be able to provide the text for a new tile and choose how its audio is created.

The app is a single Full edition, for newer, more capable Android devices. In edit mode, a user should be able to supply text for a new tile and choose from:
- Robot text-to-speech.
- **Sonia**, a British female AI voice.
- A British male AI voice, which is still to be selected.
- Their own uploaded audio in MP3, WAV, or M4A format.

The choice of the British male AI voice remains an open product decision and should not be assumed or fixed without confirmation.

## Shared product principles

- Keep the core communication experience simple, predictable, and easy to understand.
- Prioritise accessibility over decorative complexity.
- Treat soundboard users respectfully and design for dignity and independence.
- Keep the app free to users.
- Make customisation approachable for non-technical parents, relatives, carers, and support workers.

## Scope boundary for AI assistants

This document defines product context only. Do not infer implementation choices from it.

Wait for the separate implementation prompt before deciding matters such as project structure, user-interface design, device requirements, speech providers, AI services, offline behaviour, data storage, permissions, audio processing, or release strategy.
